using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(CategoryName), "category")]
[QueryProperty(nameof(SearchText), "search")]
public partial class CategoryViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly LocationService _location;

    [ObservableProperty] string _categoryName = string.Empty;
    [ObservableProperty] string _displayTitle = string.Empty;
    [ObservableProperty] string _searchText = string.Empty;

    // ── Pagination (تحميل تدريجي زي التطبيقات الكبيرة) ──────────
    // كانت الصفحة بتجيب أول صفحة بس من السيرفر (20 محل) وتقف عندها؛ أي محل
    // بعد كده مكنش بيظهر خالص. دلوقتي لما اليوزر ينزل تحت، بنجيب الصفحة اللي
    // بعدها ونضيفها، لغاية ما السيرفر يقول مفيش صفحات زيادة.
    const int PageSize = 20;
    int _currentPage = 1;
    bool _hasMore = true;
    [ObservableProperty] bool _isLoadingMore;

    // ── Banners (بنر أعلى القسم: بيظهر بس لو البنر بيوجّه لمحل من نفس القسم وجوه الزون) ──
    [ObservableProperty] int _currentBannerIndex;
    int _bannerLoadId;
    public ObservableCollection<Banner> Banners { get; } = new();


    /// <summary>نفس الـ Placeholder المستخدم في صفحة الرئيسية</summary>
    public string SearchHint => LocalizationService.Get("SearchPlaceholder");

    public ObservableCollection<Restaurant> Items { get; } = new();

    /// <summary>True لما تبقى القايمة فاضية بعد التحميل (مش وانت لسه بتحمل)</summary>
    public bool HasNoResults => !IsBusy && Items.Count == 0;

    public CategoryViewModel(ApiService api, LocationService location)
    {
        _api = api;
        _location = location;
        Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoResults));
    }

    partial void OnCategoryNameChanged(string value) => RefreshTitle();

    partial void OnSearchTextChanged(string value) => RefreshTitle();

    /// <summary>
    /// العنوان بيتحدد حسب الكاتيجوري، أو "نتائج البحث" لو جاي من السيرش من غير كاتيجوري محددة
    /// </summary>
    void RefreshTitle()
    {
        // نستخدم LocalizationService عشان يدعم Arabic/English
        var locKey = CategoryName switch
        {
            "Restaurants" => "Cat_Restaurants",
            "Grocery" => "Cat_Grocery",
            "Pharmacy" => "Cat_Pharmacy",
            "Vegetables" => "Cat_Vegetables",
            "Accessories" => "Cat_Accessories",
            "Supermarket" => "Cat_Supermarket",
            "Drinks" => "Cat_Drinks",
            _ => null
        };

        DisplayTitle = locKey != null
            ? LocalizationService.Get(locKey)
            : !string.IsNullOrWhiteSpace(SearchText)
                ? LocalizationService.Get("SearchResults")
                : LocalizationService.Get("Cat_All");
    }

    [RelayCommand]
    async Task GoBack()
    {
        IsBusy = true;
        await Task.Yield();
        try { await Shell.Current.GoToAsync(".."); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsBusy = true;
        _currentPage = 1;
        _hasMore = true;
        try
        {
            // بنبعت lat/lng للـ API مباشرة — هو بيفلتر جوه الزون (MaxDeliveryZoneKm) اللي حدده الأدمن
            await _location.RefreshZoneAsync(_api);
            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;

            var result = await _api.GetRestaurantsAsync(
                search: SearchText,
                category: CategoryName,
                lat: lat,
                lng: lng,
                radiusKm: _location.ZoneRadiusKm,
                minRating: 0.0,
                sortBy: "rating",
                page: _currentPage,
                pageSize: PageSize);

            Items.Clear();
            foreach (var x in result?.Data ?? new())
                Items.Add(x);

            _hasMore = result != null
                && (result.TotalPages.HasValue ? _currentPage < result.TotalPages.Value : result.Data.Count == PageSize);

            // البنرات بتتحمّل في الخلفية عشان متأخرش ظهور قايمة المحلات
            _ = LoadBannersAsync(lat, lng, ++_bannerLoadId);
        }
        finally { IsBusy = false; OnPropertyChanged(nameof(HasNoResults)); }
    }

    // ══════════════════════════════════════════════════════════════
    //  Banners
    //  - السيرفر بيفلتر البنرات بالزون (lat/lng/radiusKm) زي الرئيسية بالظبط
    //  - هنا بنكمّل الفلترة: بنسيب بس البنرات اللي ActionUrl بتاعها "restaurant/{id}"
    //    (أو "store/{id}") والمحل ده من نفس القسم المفتوح وجوه الزون
    //  - لو مفيش بنر مناسب، البنر مبيظهرش خالص
    // ══════════════════════════════════════════════════════════════
    async Task LoadBannersAsync(double? lat, double? lng, int loadId)
    {
        try
        {
            var category = CategoryName;
            // صفحة بحث (من غير قسم) أو بحث جوه القسم → من غير بنر
            if (string.IsNullOrWhiteSpace(category) || !string.IsNullOrWhiteSpace(SearchText))
            {
                if (loadId == _bannerLoadId) ClearBanners();
                return;
            }

            var radius = _location.ZoneRadiusKm;
            var banners = await _api.GetBannersAsync(lat, lng, radius) ?? new();

            var candidates = banners
                .Select(b => (Banner: b, StoreId: TryGetStoreId(b.ActionUrl)))
                .Where(x => x.StoreId.HasValue)
                .ToList();

            if (candidates.Count == 0)
            {
                if (loadId == _bannerLoadId) ClearBanners();
                return;
            }

            var ids = candidates.Select(x => x.StoreId!.Value).Distinct().ToList();
            var stores = await Task.WhenAll(ids.Select(id => _api.GetRestaurantAsync(id, lat, lng)));
            var byId = new Dictionary<int, Restaurant>();
            for (int i = 0; i < ids.Count; i++)
                if (stores[i] != null) byId[ids[i]] = stores[i]!;

            var visible = candidates
                .Where(x => byId.TryGetValue(x.StoreId!.Value, out var r)
                            && MatchesCategory(r, category)
                            && IsInsideZone(r, lat, lng, radius))
                .Select(x => x.Banner)
                .ToList();

            // لو اتعمل تحميل أحدث (أو القسم اتغير) نتجاهل النتيجة دي
            if (loadId != _bannerLoadId || CategoryName != category) return;

            Banners.Clear();
            foreach (var b in visible) Banners.Add(b);
            if (CurrentBannerIndex >= Banners.Count) CurrentBannerIndex = 0;
        }
        catch (Exception ex)
        {
            // البنر ميبوظش الصفحة لو حصل أي خطأ
            System.Diagnostics.Debug.WriteLine($"LoadBannersAsync failed: {ex}");
        }
    }

    void ClearBanners()
    {
        Banners.Clear();
        CurrentBannerIndex = 0;
    }

    /// <summary>"restaurant/5" أو "store/5" → 5، أي صيغة تانية (رابط خارجي، category/...) → null</summary>
    static int? TryGetStoreId(string? actionUrl)
    {
        var target = actionUrl?.Trim();
        if (string.IsNullOrWhiteSpace(target)) return null;
        var parts = target.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        var type = parts[0].ToLowerInvariant();
        if (type != "restaurant" && type != "store") return null;
        return int.TryParse(parts[1], out var id) ? id : null;
    }

    // نفس شرط السيرفر في /api/restaurants?category=... (StoreType == category)
    // عشان البنر مايظهرش إلا لمحل هيظهر فعلاً في قايمة القسم ده
    static bool MatchesCategory(Restaurant r, string category)
        => string.Equals(r.StoreType?.Trim(), category.Trim(), StringComparison.OrdinalIgnoreCase);

    static bool IsInsideZone(Restaurant r, double? lat, double? lng, double radiusKm)
    {
        if (!lat.HasValue || !lng.HasValue) return true;   // مفيش موقع محدد → زي الرئيسية، مفيش فلترة
        double? dist = r.DistanceKm;
        if (!dist.HasValue && (r.Latitude != 0 || r.Longitude != 0))
            dist = LocationService.DistanceKm(lat.Value, lng.Value, r.Latitude, r.Longitude);
        return !dist.HasValue || dist.Value <= radiusKm + 0.05;
    }

    /// <summary>الدوس على البنر → صفحة المحل اللي البنر موجّه ليه</summary>
    [RelayCommand]
    async Task OpenBanner(Banner? banner)
    {
        try
        {
            var id = TryGetStoreId(banner?.ActionUrl);
            if (!id.HasValue) return;

            IsBusy = true;
            await Task.Yield();
            await Shell.Current.GoToAsync($"RestaurantPage?id={id.Value}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenBanner failed: {ex}");
        }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// بتتنفذ تلقائيًا لما اليوزر يقرب من آخر عنصر في القايمة (RemainingItemsThreshold
    /// في الـ XAML) — بنجيب الصفحة اللي بعدها ونضيفها بدل ما نستنى اليوزر يدوس حاجة.
    /// </summary>
    [RelayCommand]
    async Task LoadMoreAsync()
    {
        if (IsBusy || IsLoadingMore || !_hasMore) return;
        IsLoadingMore = true;
        try
        {
            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;

            var nextPage = _currentPage + 1;
            var result = await _api.GetRestaurantsAsync(
                search: SearchText,
                category: CategoryName,
                lat: lat,
                lng: lng,
                radiusKm: _location.ZoneRadiusKm,
                minRating: 0.0,
                sortBy: "rating",
                page: nextPage,
                pageSize: PageSize);

            if (result?.Data is { Count: > 0 })
            {
                foreach (var x in result.Data) Items.Add(x);
                _currentPage = nextPage;
            }

            _hasMore = result != null
                && (result.TotalPages.HasValue ? _currentPage < result.TotalPages.Value : result.Data.Count == PageSize);
        }
        finally { IsLoadingMore = false; }
    }

    /// <summary>بيتنفذ لما المستخدم يدوس Enter/بحث في مربع السيرش اللي فوق</summary>
    [RelayCommand]
    Task SearchAsync() => LoadAsync();

    [RelayCommand]
    static Task OpenRestaurant(Restaurant r)
        => Shell.Current.GoToAsync($"RestaurantPage?id={r.Id}");
}