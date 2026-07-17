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
    async Task LoadAsync()
    {
        IsBusy = true;
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

            Restaurants.Clear();
            foreach (var x in restaurantsTask.Result?.Data ?? new()) Restaurants.Add(x);
        }
        finally { IsBusy = false; IsRefreshing = false; OnPropertyChanged(nameof(HasNoResults)); }
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
    static Task OpenRestaurant(Restaurant r)
        => Shell.Current.GoToAsync($"RestaurantPage?id={r.Id}");

    [RelayCommand]
    static Task OpenCart() => Shell.Current.GoToAsync("CartPage");

    [RelayCommand]
    static Task OpenCoupons() => Shell.Current.GoToAsync(nameof(Views.CouponsPage));

    [RelayCommand]
    static Task OpenRewards() => Shell.Current.GoToAsync(nameof(Views.RewardsPage));
}