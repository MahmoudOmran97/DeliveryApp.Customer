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
        }
        finally { IsBusy = false; OnPropertyChanged(nameof(HasNoResults)); }
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