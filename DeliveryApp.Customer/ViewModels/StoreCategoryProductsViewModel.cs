// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / StoreCategoryProductsViewModel.cs
// ═══════════════════════════════════════════════════════════════
// ✅ FEATURE: صفحة منتجات قسم واحد جوه محل (سوبر ماركت / صيدلية) — بتتفتح لما
// اليوزر يدوس على قسم من شبكة الأقسام في صفحة المحل. فيها:
//   • شريط أقسام أفقي علوي للتنقل بين الأقسام من غير الرجوع للخلف.
//   • فرز: الأفضل مبيعًا (افتراضي) / الأقل سعرًا / الأعلى سعرًا.
//   • فلتر: عرض المنتجات اللي عليها خصم بس.
//   • لما تضيف منتج للسلة، بيظهر شريط "بيتطلب مع" باقتراحات حقيقية جايه من
//     تحليل أوردرات فعلية (endpoint: /related).
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(RestaurantId), "restaurantId")]
[QueryProperty(nameof(InitialCategoryId), "categoryId")]
[QueryProperty(nameof(DeliveryFeeParam), "deliveryFee")]
[QueryProperty(nameof(StoreNameParam), "storeName")]
public partial class StoreCategoryProductsViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly CartService _cart;

    [ObservableProperty] int _restaurantId;
    [ObservableProperty] int _initialCategoryId;
    [ObservableProperty] string _deliveryFeeParam = "15";
    [ObservableProperty] string _storeNameParam = "";
    [ObservableProperty] int _cartCount;

    [ObservableProperty] Category? _selectedCategory;

    // "best" | "priceAsc" | "priceDesc"
    [ObservableProperty] string _sortBy = "best";
    [ObservableProperty] bool _discountOnly;

    // شريط اقتراحات "بيتطلب مع" بعد الإضافة للسلة
    [ObservableProperty] bool _showSuggestions;
    [ObservableProperty] Product? _lastAddedProduct;

    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Product> DisplayedProducts { get; } = new();
    public ObservableCollection<Product> SuggestedProducts { get; } = new();

    List<Category> _allCategories = new();
    decimal _deliveryFee = 15m;

    public bool IsSortBest => SortBy == "best";
    public bool IsSortPriceAsc => SortBy == "priceAsc";
    public bool IsSortPriceDesc => SortBy == "priceDesc";
    public bool HasNoProducts => !IsBusy && SelectedCategory != null && DisplayedProducts.Count == 0;

    public StoreCategoryProductsViewModel(ApiService api, CartService cart)
    {
        _api = api;
        _cart = cart;
        _cart.CartChanged += () => CartCount = _cart.TotalCount;
        CartCount = _cart.TotalCount;
        DisplayedProducts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoProducts));
    }

    partial void OnRestaurantIdChanged(int value)
    {
        IsBusy = true;
        LoadCommand.Execute(null);
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        foreach (var c in Categories) c.IsSelectedForChip = ReferenceEquals(c, value);
        ApplyFilters();
    }
    partial void OnSortByChanged(string value)
    {
        OnPropertyChanged(nameof(IsSortBest));
        OnPropertyChanged(nameof(IsSortPriceAsc));
        OnPropertyChanged(nameof(IsSortPriceDesc));
        ApplyFilters();
    }
    partial void OnDiscountOnlyChanged(bool value) => ApplyFilters();

    [RelayCommand]
    async Task LoadAsync()
    {
        if (RestaurantId == 0) return;
        IsBusy = true;
        try
        {
            decimal.TryParse(DeliveryFeeParam, NumberStyles.Any, CultureInfo.InvariantCulture, out _deliveryFee);

            var menu = await _api.GetMenuAsync(RestaurantId);
            _allCategories = menu ?? new();

            Categories.Clear();
            foreach (var c in _allCategories) Categories.Add(c);

            SelectedCategory = Categories.FirstOrDefault(c => c.Id == InitialCategoryId) ?? Categories.FirstOrDefault();
            ApplyFilters();
        }
        finally { IsBusy = false; OnPropertyChanged(nameof(HasNoProducts)); }
    }

    [RelayCommand]
    void SelectCategory(Category c) => SelectedCategory = c;

    [RelayCommand]
    void SetSort(string sort) => SortBy = sort;

    [RelayCommand]
    void ToggleDiscountOnly() => DiscountOnly = !DiscountOnly;

    void ApplyFilters()
    {
        DisplayedProducts.Clear();
        if (SelectedCategory == null) { OnPropertyChanged(nameof(HasNoProducts)); return; }

        IEnumerable<Product> q = SelectedCategory.Products;
        if (DiscountOnly) q = q.Where(p => p.HasDiscount);

        q = SortBy switch
        {
            "priceAsc" => q.OrderBy(p => p.EffectivePrice),
            "priceDesc" => q.OrderByDescending(p => p.EffectivePrice),
            _ => q.OrderByDescending(p => p.IsBestSeller).ThenByDescending(p => p.SalesCount)
        };

        foreach (var p in q) DisplayedProducts.Add(p);
        OnPropertyChanged(nameof(HasNoProducts));
    }

    [RelayCommand]
    async Task ProductTapped(Product p)
    {
        IsBusy = true;
        await Task.Yield();
        try
        {
            var json = Uri.EscapeDataString(JsonSerializer.Serialize(p));
            var fee = _deliveryFee.ToString(CultureInfo.InvariantCulture);
            await Shell.Current.GoToAsync(
                $"ProductOptionsPage?product={json}&restaurantId={RestaurantId}&deliveryFee={fee}");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task AddToCart(Product p)
    {
        var ok = _cart.AddItem(RestaurantId, p, deliveryFee: _deliveryFee);
        if (!ok)
        {
            bool clear = await Shell.Current.DisplayAlert(
                LocalizationService.Get("DifferentRestaurant"),
                LocalizationService.Get("DifferentRestaurantMsg"),
                LocalizationService.Get("YesClear"),
                LocalizationService.Get("Cancel"));

            if (!clear) return;
            _cart.Clear();
            _cart.AddItem(RestaurantId, p, deliveryFee: _deliveryFee);
        }

        CartCount = _cart.TotalCount;
        await LoadSuggestionsAsync(p);
    }

    async Task LoadSuggestionsAsync(Product p)
    {
        try
        {
            var related = await _api.GetRelatedProductsAsync(RestaurantId, p.Id, 6);
            SuggestedProducts.Clear();
            if (related != null)
                foreach (var r in related) SuggestedProducts.Add(r);

            LastAddedProduct = p;
            ShowSuggestions = SuggestedProducts.Count > 0;
        }
        catch
        {
            // مفيش داعي نوقف اليوزر لو الاقتراحات فشلت — المنتج أصلاً اتضاف للسلة بنجاح
            ShowSuggestions = false;
        }
    }

    [RelayCommand]
    void DismissSuggestions() => ShowSuggestions = false;

    [RelayCommand]
    async Task AddSuggested(Product p) => await AddToCart(p);

    // ملاحظة: GoBackCommand موروث من BaseViewModel، مفيش داعي نعرّفه تاني هنا.
    [RelayCommand]
    async Task OpenCart()
    {
        IsBusy = true;
        await Task.Yield();
        try { await Shell.Current.GoToAsync("CartPage"); }
        finally { IsBusy = false; }
    }
}
