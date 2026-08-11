// ═══════════════════════════════════════════════════════════════
// ViewModels / HomeViewModel.cs  – Updated with:
//   1. Location picker (tap header to set location)
//   2. Category filter (All / Restaurants / Pharmacy / Grocery / etc.)
//   3. Top-rated section title (4+ stars, max 5 results)
//   4. 10km zone restriction
//   5. Navigate back to home on category tap
// ═══════════════════════════════════════════════════════════════
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly CartService _cart;
    readonly LocationService _location;

    // ── State ──────────────────────────────────────────────────
    [ObservableProperty] string _searchText = string.Empty;
    [ObservableProperty] bool _isRefreshing;
    [ObservableProperty] string _userName = string.Empty;
    [ObservableProperty] int _cartCount;
    [ObservableProperty] int _currentBannerIndex;

    /// <summary>Category currently selected – null means "All"</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SectionTitle))]
    [NotifyPropertyChangedFor(nameof(IsCatAll))]
    [NotifyPropertyChangedFor(nameof(IsCatRestaurants))]
    [NotifyPropertyChangedFor(nameof(IsCatPharmacy))]
    [NotifyPropertyChangedFor(nameof(IsCatGrocery))]
    [NotifyPropertyChangedFor(nameof(IsCatSupermarket))]
    [NotifyPropertyChangedFor(nameof(IsCatVegetables))]
    [NotifyPropertyChangedFor(nameof(IsCatDrinks))]
    [NotifyPropertyChangedFor(nameof(IsCatAccessories))]
    string? _selectedCategory;

    [ObservableProperty] string _locationLabel = string.Empty;
    [ObservableProperty] bool _hasLocation;

    // ── Greeting / hints ──────────────────────────────────────
    public string GreetingPrefix => LocalizationService.Current.TwoLetterISOLanguageName == "ar"
        ? "أهلاً، " : "Hey, ";

    public string SearchHint => LocalizationService.Get("SearchPlaceholder");

    /// <summary>
    /// العنوان الديناميكي للسكشن بناء على الـ category المختارة
    /// "أفضل المطاعم" / "أفضل الصيدليات" / etc.
    /// </summary>
    public string SectionTitle
    {
        get
        {
            var key = SelectedCategory switch
            {
                "Restaurants" => "TopRatedRestaurants",
                "Pharmacy" => "TopRatedPharmacy",
                "Grocery" => "TopRatedGrocery",
                "Supermarket" => "TopRatedGrocery",
                "Vegetables" => "TopRatedVegetables",
                "Drinks" => "TopRatedDrinks",
                "Accessories" => "TopRatedAccessories",
                _ => "TopRatedNearYou"
            };
            return LocalizationService.Get(key);
        }
    }

    // ── Per-category IsSelected (for chip colors in XAML) ─────────
    public bool IsCatAll => SelectedCategory == null;
    public bool IsCatRestaurants => SelectedCategory == "Restaurants";
    public bool IsCatPharmacy => SelectedCategory == "Pharmacy";
    public bool IsCatGrocery => SelectedCategory == "Grocery";
    public bool IsCatSupermarket => SelectedCategory == "Supermarket";
    public bool IsCatVegetables => SelectedCategory == "Vegetables";
    public bool IsCatDrinks => SelectedCategory == "Drinks";
    public bool IsCatAccessories => SelectedCategory == "Accessories";

    public ObservableCollection<Restaurant> Restaurants { get; } = new();

    /// <summary>True when location is set but list is empty (after load)</summary>
    public bool HasNoResults => !IsBusy && HasLocation && Restaurants.Count == 0;
    public ObservableCollection<Banner> Banners { get; } = new();

    // ── Categories list ────────────────────────────────────────
    // كل category: Key (للـ API) + LabelKey (للترجمة) + Emoji
    public record CategoryItem(string? Key, string LabelKey, string Emoji);

    public List<CategoryItem> Categories { get; } = new()
    {
        new(null,           "Cat_All",         "🏠"),
        new("Restaurants",  "Cat_Restaurants", "🍽️"),
        new("Pharmacy",     "Cat_Pharmacy",    "💊"),
        new("Grocery",      "Cat_Grocery",     "🛒"),
        new("Supermarket",  "Cat_Supermarket", "🏪"),
        new("Vegetables",   "Cat_Vegetables",  "🥦"),
        new("Drinks",       "Cat_Drinks",      "🧃"),
        new("Accessories",  "Cat_Accessories", "👜"),
    };

    // ── DI ────────────────────────────────────────────────────
    public HomeViewModel(ApiService api, AuthService auth, CartService cart, LocationService location)
    {
        _api = api;
        _cart = cart;
        _location = location;

        UserName = auth.GetUserName().Split(' ')[0];
        _cart.CartChanged += () => CartCount = _cart.TotalCount;

        // Notify HasNoResults when list or busy-state changes
        Restaurants.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoResults));

        // Refresh when location changes
        _location.LocationChanged += OnLocationChanged;
        RefreshLocationLabel();
    }

    // ── Location helpers ──────────────────────────────────────
    void RefreshLocationLabel()
    {
        HasLocation = _location.HasLocation;
        LocationLabel = _location.HasLocation
            ? _location.AddressLabel
            : LocalizationService.Get("TapToSetLocation");
        OnPropertyChanged(nameof(SectionTitle));
    }

    void OnLocationChanged()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            RefreshLocationLabel();
            await LoadAsync();
        });
    }

    // ── Commands ──────────────────────────────────────────────

    [RelayCommand]
    async Task OpenLocationPicker()
    {
        // نفتح صفحة اختيار الموقع — الـ result بييجي عبر QueryParam
        await Shell.Current.GoToAsync($"HomeLocationPickerPage");
    }

    /// <summary>Called when user taps a category chip</summary>
    [RelayCommand]
    async Task SelectCategory(string? key)
    {
        if (key == null)
        {
            // "الكل" → نرجع للحالة العادية في نفس الصفحة
            SelectedCategory = null;
            await LoadAsync();
        }
        else
        {
            // category محددة → ننتقل لصفحة منفصلة بتعرض كل المحلات من النوع ده
            await Shell.Current.GoToAsync($"{nameof(Views.CategoryPage)}?category={key}");
        }
    }

    [RelayCommand]
    async Task LoadAsync() => await LoadInternalAsync(silent: false);

    // ✅ FIX: تحديث هادئ للبيانات بيتنفذ لما نرجع للصفحة تاني، من غير ما يوقف
    // الـ Spinner على كل المحتوى أو يقفل الكاروسيل وهي شغالة (سبب رئيسي في
    // الإحساس بـ"قفشة"/تعليق كل ما ترجع للهوم)
    public async Task RefreshSilentlyAsync() => await LoadInternalAsync(silent: true);

    async Task LoadInternalAsync(bool silent)
    {
        if (!silent) IsBusy = true;
        try
        {
            // Banners (no location filter)
            var bannersTask = _api.GetBannersAsync();

            // Restaurants: location + category + top-rated (≥4 stars, max 5)
            double? lat = _location.HasLocation ? _location.Latitude : null;
            double? lng = _location.HasLocation ? _location.Longitude : null;

            var restaurantsTask = _api.GetRestaurantsAsync(
                search: SearchText,
                sortBy: "rating",
                page: 1,
                lat: lat,
                lng: lng,
                radiusKm: 10.0,
                category: SelectedCategory,
                minRating: 4.0,
                pageSize: 5);

            await Task.WhenAll(bannersTask, restaurantsTask);

            Banners.Clear();
            foreach (var b in bannersTask.Result ?? new()) Banners.Add(b);
            // ✅ FIX: لو الـ index الحالي بقى برا حدود القائمة الجديدة بعد التحديث
            // (مثلاً كان على آخر بانر واتقلل عددهم)، نرجعه لـ 0 بدل ما يعمل مشكلة
            // في الـ CarouselView.
            if (CurrentBannerIndex >= Banners.Count) CurrentBannerIndex = 0;

            Restaurants.Clear();
            foreach (var x in restaurantsTask.Result?.Data ?? new()) Restaurants.Add(x);
        }
        finally { if (!silent) IsBusy = false; IsRefreshing = false; OnPropertyChanged(nameof(HasNoResults)); }
    }

    [RelayCommand]
    async Task RefreshAsync() { IsRefreshing = true; await LoadAsync(); }

    /// <summary>
    /// لما المستخدم يدوس بحث من الرئيسية، بنودّيه على صفحة المحلات (CategoryPage)
    /// وهي اللي هتعرض النتائج، بنفس الكاتيجوري المختارة لو فيه واحدة
    /// </summary>
    [RelayCommand]
    async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return;

        var category = SelectedCategory ?? string.Empty;
        await Shell.Current.GoToAsync(
            $"{nameof(Views.CategoryPage)}?category={Uri.EscapeDataString(category)}&search={Uri.EscapeDataString(SearchText)}");
    }

    [RelayCommand]
    async Task OpenRestaurant(Restaurant r)
    {
        IsBusy = true;
        await Task.Yield();
        try { await Shell.Current.GoToAsync($"RestaurantPage?id={r.Id}"); }
        finally { IsBusy = false; }
    }

    /// <summary>
    /// لما المستخدم يدوس على البنر، بنفك ActionUrl اللي جاي من الأدمن
    /// (بصيغة "restaurant/5" أو "category/Pharmacy" أو رابط خارجي كامل) ونوديه المكان الصح
    /// </summary>
    [RelayCommand]
    async Task OpenBanner(Banner? banner)
    {
        try
        {
            var target = banner?.ActionUrl?.Trim();
            if (string.IsNullOrWhiteSpace(target)) return;

            IsBusy = true;
            await Task.Yield();
            // رابط خارجي كامل → افتحه في المتصفح
            if (target.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                await Launcher.OpenAsync(target);
                return;
            }

            var parts = target.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            var type = parts[0].ToLowerInvariant();
            var value = parts[1];

            switch (type)
            {
                case "restaurant":
                case "store":
                    await Shell.Current.GoToAsync($"RestaurantPage?id={value}");
                    break;

                case "category":
                    await Shell.Current.GoToAsync($"{nameof(Views.CategoryPage)}?category={Uri.EscapeDataString(value)}");
                    break;
            }
        }
        catch (Exception ex)
        {
            // أي مشكلة في رابط البنر (رابط غلط، معرّف مش موجود...) متكسرش التطبيق
            System.Diagnostics.Debug.WriteLine($"OpenBanner failed: {ex}");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task OpenCart()
    {
        IsBusy = true;
        await Task.Yield();
        try { await Shell.Current.GoToAsync("CartPage"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task OpenCoupons()
    {
        IsBusy = true;
        await Task.Yield();
        try { await Shell.Current.GoToAsync(nameof(Views.CouponsPage)); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task OpenRewards()
    {
        IsBusy = true;
        await Task.Yield();
        try { await Shell.Current.GoToAsync(nameof(Views.RewardsPage)); }
        finally { IsBusy = false; }
    }
}