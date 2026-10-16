// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / SearchViewModel.cs
// ═══════════════════════════════════════════════════════════════
// ✅ FEATURE: صفحة البحث الشاملة — بتتفتح أول ما العميل يدوس على مربع السيرش في الرئيسية.
//   • بحث في المحلات + المنتجات (مع اسم المحل التابع له المنتج) + العروض.
//   • كل النتايج جوه نطاق التوصيل (الزون) بتاع العميل.
//   • لو العميل كتب "عروض/عرض/خصم/offer..." بنعرض كل العروض المتاحة.
//   • بحث تلقائي أثناء الكتابة (debounce 400ms) + زرار بحث الكيبورد للبحث الفوري.
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Helpers;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class SearchViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly LocationService _location;
    readonly CartService _cart;

    const int MinQueryLength = 2;
    const int DebounceMs = 400;

    // كلمات لو العميل كتبها نعرض كل العروض (بعد التطبيع)
    static readonly string[] OfferKeywords =
        new[] { "عرض", "عروض", "خصم", "خصومات", "تخفيض", "تخفيضات", "offer", "offers", "deal", "deals", "discount", "sale" }
            .Select(SearchTextHelper.Normalize).ToArray();

    [ObservableProperty] string _query = string.Empty;
    [ObservableProperty] bool _isSearching;
    [ObservableProperty] bool _hasSearched;

    // all | stores | products | offers
    [ObservableProperty] string _selectedTab = "all";

    public ObservableCollection<Restaurant> Stores { get; } = new();
    public ObservableCollection<ProductSearchResult> Products { get; } = new();
    public ObservableCollection<Deal> Deals { get; } = new();

    public string SearchHint => LocalizationService.Get("SearchPageHint");

    public bool IsTabAll => SelectedTab == "all";
    public bool IsTabStores => SelectedTab == "stores";
    public bool IsTabProducts => SelectedTab == "products";
    public bool IsTabOffers => SelectedTab == "offers";

    public bool ShowStores => (IsTabAll || IsTabStores) && Stores.Count > 0;
    public bool ShowProducts => (IsTabAll || IsTabProducts) && Products.Count > 0;
    public bool ShowDeals => (IsTabAll || IsTabOffers) && Deals.Count > 0;

    public bool HasAnyResults => Stores.Count > 0 || Products.Count > 0 || Deals.Count > 0;
    public bool ShowTabs => HasSearched && HasAnyResults;
    public bool ShowEmpty => HasSearched && !IsSearching && !HasAnyResults;
    public bool ShowHint => !HasSearched && !IsSearching;
    public bool HasQuery => !string.IsNullOrEmpty(Query);

    CancellationTokenSource? _cts;

    public SearchViewModel(ApiService api, LocationService location, CartService cart)
    {
        _api = api;
        _location = location;
        _cart = cart;

        Stores.CollectionChanged += (_, _) => NotifyResultsChanged();
        Products.CollectionChanged += (_, _) => NotifyResultsChanged();
        Deals.CollectionChanged += (_, _) => NotifyResultsChanged();
    }

    void NotifyResultsChanged()
    {
        OnPropertyChanged(nameof(ShowStores));
        OnPropertyChanged(nameof(ShowProducts));
        OnPropertyChanged(nameof(ShowDeals));
        OnPropertyChanged(nameof(HasAnyResults));
        OnPropertyChanged(nameof(ShowTabs));
        OnPropertyChanged(nameof(ShowEmpty));
    }

    partial void OnSelectedTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsTabAll));
        OnPropertyChanged(nameof(IsTabStores));
        OnPropertyChanged(nameof(IsTabProducts));
        OnPropertyChanged(nameof(IsTabOffers));
        NotifyResultsChanged();
    }

    partial void OnHasSearchedChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowHint));
        NotifyResultsChanged();
    }

    partial void OnIsSearchingChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowHint));
        OnPropertyChanged(nameof(ShowEmpty));
    }

    partial void OnQueryChanged(string value)
    {
        OnPropertyChanged(nameof(HasQuery));

        _cts?.Cancel();
        var text = value?.Trim() ?? string.Empty;

        if (text.Length < MinQueryLength)
        {
            ClearResults();
            return;
        }

        _cts = new CancellationTokenSource();
        _ = RunSearchAsync(text, DebounceMs, _cts.Token);
    }

    void ClearResults()
    {
        Stores.Clear();
        Products.Clear();
        Deals.Clear();
        IsSearching = false;
        HasSearched = false;
    }

    // ── زرار البحث في الكيبورد: بحث فوري من غير انتظار الـ debounce ──
    [RelayCommand]
    void SearchNow()
    {
        var text = Query?.Trim() ?? string.Empty;
        if (text.Length < MinQueryLength) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = RunSearchAsync(text, 0, _cts.Token);
    }

    [RelayCommand]
    void ClearQuery() => Query = string.Empty;

    [RelayCommand]
    void SelectTab(string tab) => SelectedTab = tab;

    async Task RunSearchAsync(string text, int delayMs, CancellationToken ct)
    {
        try
        {
            if (delayMs > 0) await Task.Delay(delayMs, ct);
            IsSearching = true;

            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;
            var radius = _location.ZoneRadiusKm;

            var storesTask = _api.GetRestaurantsAsync(
                search: text, sortBy: "rating", page: 1,
                lat: lat, lng: lng, radiusKm: radius, pageSize: 10);
            var productsTask = _api.SearchProductsAsync(text, lat, lng, radius, pageSize: 30);
            var dealsTask = _api.GetDealsAsync(lat, lng, radius);

            try { await Task.WhenAll(storesTask, productsTask, dealsTask); }
            catch { /* ApiService بيبلع الأخطاء، ده مجرد احتياط */ }

            // نتيجة قديمة (العميل كمّل كتابة) — نتجاهلها
            if (ct.IsCancellationRequested) return;

            var stores = storesTask.IsCompletedSuccessfully ? storesTask.Result?.Data : null;
            var products = productsTask.IsCompletedSuccessfully ? productsTask.Result?.Data : null;
            var allDeals = dealsTask.IsCompletedSuccessfully ? dealsTask.Result : null;

            // العروض: بحث محلي في العروض اللي جاية أصلاً مفلترة بالزون
            var nq = SearchTextHelper.Normalize(text);
            var wantsOffers = OfferKeywords.Any(k => nq.Contains(k, StringComparison.Ordinal));
            var matchedDeals = (allDeals ?? new List<Deal>())
                .Where(d => wantsOffers
                    || SearchTextHelper.Matches(d.Title, nq)
                    || SearchTextHelper.Matches(d.Description, nq)
                    || SearchTextHelper.Matches(d.ProductName, nq)
                    || SearchTextHelper.Matches(d.RestaurantName, nq)
                    || SearchTextHelper.Matches(d.BadgeText, nq))
                .OrderBy(d => d.SortOrder)
                .ToList();

            Stores.Clear();
            foreach (var s in stores ?? new()) Stores.Add(s);

            Products.Clear();
            foreach (var p in products ?? new()) Products.Add(p);

            Deals.Clear();
            foreach (var d in matchedDeals) Deals.Add(d);

            // لو التاب الحالي مفيهوش نتايج ورجّعنا نتايج في تاب تاني، نرجع لـ "الكل"
            if (SelectedTab != "all" && !(SelectedTab switch
            {
                "stores" => Stores.Count > 0,
                "products" => Products.Count > 0,
                "offers" => Deals.Count > 0,
                _ => true
            }))
                SelectedTab = "all";

            HasSearched = true;
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (!ct.IsCancellationRequested) IsSearching = false;
        }
    }

    // ── فتح محل ──────────────────────────────────────────────
    [RelayCommand]
    async Task OpenStore(Restaurant? r)
    {
        if (r == null) return;
        await NavigateAsync($"RestaurantPage?id={r.Id}");
    }

    // الدوس على اسم المحل جوه نتيجة المنتج
    [RelayCommand]
    async Task OpenProductStore(ProductSearchResult? p)
    {
        if (p == null) return;
        await NavigateAsync($"RestaurantPage?id={p.RestaurantId}");
    }

    // ── فتح منتج (صفحة الخيارات/الإضافة للسلة) ──────────────────
    [RelayCommand]
    async Task OpenProduct(ProductSearchResult? p)
    {
        if (p == null) return;
        IsBusy = true;
        await Task.Yield();
        try
        {
            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;

            // نجيب المنتج كامل (فيه الـ Variants) وسعر التوصيل الفعلي للمحل
            var productTask = _api.GetProductAsync(p.Id);
            var storeTask = _api.GetRestaurantAsync(p.RestaurantId, lat, lng);
            try { await Task.WhenAll(productTask, storeTask); } catch { }

            var product = productTask.IsCompletedSuccessfully ? productTask.Result : null;
            if (product == null)
            {
                // مقدرناش نجيب المنتج — نوديه على صفحة المحل بدل ما نسيبه من غير رد
                await Shell.Current.GoToAsync($"RestaurantPage?id={p.RestaurantId}");
                return;
            }

            product.RestaurantId = p.RestaurantId;
            var store = storeTask.IsCompletedSuccessfully ? storeTask.Result : null;
            var fee = (store?.DeliveryFee ?? 15m).ToString(CultureInfo.InvariantCulture);
            var json = Uri.EscapeDataString(JsonSerializer.Serialize(product));

            await Shell.Current.GoToAsync(
                $"{nameof(Views.ProductOptionsPage)}?product={json}&restaurantId={p.RestaurantId}&deliveryFee={fee}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search OpenProduct failed: {ex}");
        }
        finally { IsBusy = false; }
    }

    // ── فتح عرض: نفس سلوك العروض في الرئيسية (إضافة للسلة / اختيار الخيارات) ──
    [RelayCommand]
    async Task OpenDeal(Deal? deal)
    {
        if (deal == null) return;

        // عرض عام من غير محل/منتج محدد — مفيش حاجة نفتحها
        if (!deal.RestaurantId.HasValue) return;

        IsBusy = true;
        await Task.Yield();
        try
        {
            // عرض على محل بس (من غير منتج) → نفتح المحل
            if (!deal.ProductId.HasValue)
            {
                await Shell.Current.GoToAsync($"RestaurantPage?id={deal.RestaurantId.Value}");
                return;
            }

            var product = await _api.GetProductAsync(deal.ProductId.Value);
            if (product == null) return;

            if (product.HasVariants)
            {
                var json = Uri.EscapeDataString(JsonSerializer.Serialize(product));
                await Shell.Current.GoToAsync(
                    $"{nameof(Views.ProductOptionsPage)}?product={json}&restaurantId={deal.RestaurantId.Value}");
                return;
            }

            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;
            var restaurant = await _api.GetRestaurantAsync(deal.RestaurantId.Value, lat, lng);
            var deliveryFee = restaurant?.DeliveryFee ?? 15m;

            var ok = _cart.AddItem(
                restaurantId: deal.RestaurantId.Value,
                product: product,
                qty: 1,
                notes: deal.Title,
                deliveryFee: deliveryFee,
                unitPrice: deal.DiscountedPrice,
                dealId: deal.Id);

            if (!ok)
            {
                // السلة فيها حاجة من محل تاني — نسأل العميل زي باقي صفحات التطبيق
                bool clear = await Shell.Current.DisplayAlert(
                    LocalizationService.Get("DifferentRestaurant"),
                    LocalizationService.Get("DifferentRestaurantMsg"),
                    LocalizationService.Get("YesClear"),
                    LocalizationService.Get("Cancel"));
                if (!clear) return;

                _cart.Clear();
                _cart.AddItem(
                    restaurantId: deal.RestaurantId.Value,
                    product: product,
                    qty: 1,
                    notes: deal.Title,
                    deliveryFee: deliveryFee,
                    unitPrice: deal.DiscountedPrice,
                    dealId: deal.Id);
            }

            await Shell.Current.GoToAsync("CartPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Search OpenDeal failed: {ex}");
        }
        finally { IsBusy = false; }
    }

    async Task NavigateAsync(string route)
    {
        IsBusy = true;
        await Task.Yield();
        try { await Shell.Current.GoToAsync(route); }
        finally { IsBusy = false; }
    }
}
