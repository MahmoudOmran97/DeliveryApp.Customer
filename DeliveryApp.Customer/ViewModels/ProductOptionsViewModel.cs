using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(ProductJson), "product")]
[QueryProperty(nameof(RestaurantId), "restaurantId")]
[QueryProperty(nameof(DeliveryFeeRaw), "deliveryFee")]
public partial class ProductOptionsViewModel : BaseViewModel
{
    readonly CartService _cart;

    [ObservableProperty] string _productJson = "";
    [ObservableProperty] int _restaurantId;

    // ? FIX: MAUI Shell's [QueryProperty] type-converts the raw string into the target
    // property type using CurrentCulture, NOT InvariantCulture. So when the UI language
    // is Arabic, converting "35.00" straight into a `decimal` property throws
    // FormatException, because the Arabic culture doesn't accept "." as the decimal
    // separator. Fix: receive the value as a plain `string` (no implicit conversion),
    // then parse it ourselves with CultureInfo.InvariantCulture.
    // NOTE: written manually (no [ObservableProperty]) to avoid any ambiguity with a
    // stale generated "DeliveryFee" property from a previous build.
    string _deliveryFeeRaw = "15";
    public string DeliveryFeeRaw
    {
        get => _deliveryFeeRaw;
        set
        {
            if (_deliveryFeeRaw == value) return;
            _deliveryFeeRaw = value;
            OnPropertyChanged(nameof(DeliveryFeeRaw));

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var fee))
                DeliveryFee = fee;
            else
                System.Diagnostics.Debug.WriteLine($"[ProductOptionsViewModel] Could not parse deliveryFee '{value}', keeping default {DeliveryFee}");
        }
    }

    public decimal DeliveryFee { get; private set; } = 15m;

    [ObservableProperty] Product? _product;
    [ObservableProperty] ProductVariant? _selectedVariant;
    [ObservableProperty] int _quantity = 1;

    public ObservableCollection<ProductVariant> Variants { get; } = new();

    // ✅ FIX: المنتج اللي معندوش اختيارات (Variants) مكانش ينفع يتضاف للسلة خالص
    // لأن CanAddToCart كانت بتعتمد بس على SelectedVariant != null.
    public bool HasVariants => Variants.Count > 0;
    public bool CanAddToCart => !HasVariants || SelectedVariant != null;
    public string AddButtonText => CanAddToCart
        ? LocalizationService.Get("AddToCart")
        : LocalizationService.Get("CompleteSelections");

    public ProductOptionsViewModel(CartService cart) => _cart = cart;

    partial void OnProductJsonChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            // NOTE: Shell already URL-decodes [QueryProperty] values before this setter runs.
            // Do NOT call Uri.UnescapeDataString(value) here again ? double-decoding a JSON
            // payload that contains a literal '%' (e.g. a product name/description like
            // "Discount 20%") throws a UriFormatException, which the catch below then
            // swallows silently, leaving Product null and the sheet blank.
            Product = System.Text.Json.JsonSerializer.Deserialize<Product>(value,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            LoadVariants();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProductOptionsViewModel] Failed to parse product JSON: {ex}");
        }
    }

    void LoadVariants()
    {
        Variants.Clear();
        if (Product == null) return;
        foreach (var v in Product.Variants.OrderBy(x => x.SortOrder))
            Variants.Add(v);
        OnPropertyChanged(nameof(HasVariants));
        OnPropertyChanged(nameof(CanAddToCart));
        OnPropertyChanged(nameof(AddButtonText));
    }

    partial void OnSelectedVariantChanged(ProductVariant? value)
    {
        OnPropertyChanged(nameof(CanAddToCart));
        OnPropertyChanged(nameof(AddButtonText));
    }

    [RelayCommand]
    void SelectVariant(ProductVariant variant)
    {
        foreach (var v in Variants)
            v.IsSelected = v == variant;

        SelectedVariant = variant;
    }

    [RelayCommand]
    void IncreaseQty() => Quantity++;

    [RelayCommand]
    void DecreaseQty()
    {
        if (Quantity > 1) Quantity--;
    }

    [RelayCommand]
    async Task AddToCartAsync()
    {
        if (Product == null || !CanAddToCart) return;

        // ✅ FIX: لو المنتج معندوش Variants، مفيش SelectedVariant أصلاً —
        // بنستخدم سعر المنتج نفسه (أو السعر بعد الخصم لو موجود) بدل ما نرجع من غير ما نضيف حاجة.
        var unitPrice = SelectedVariant?.Price ?? Product.DiscountedPrice ?? Product.Price;

        var ok = _cart.AddItem(
            RestaurantId, Product, Quantity,
            deliveryFee: DeliveryFee,
            variantId: SelectedVariant?.Id,
            variantName: SelectedVariant?.Name,
            unitPrice: unitPrice);

        if (!ok)
        {
            var clear = await Shell.Current.DisplayAlert(
                LocalizationService.Get("DifferentRestaurant"),
                LocalizationService.Get("DifferentRestaurantMsg"),
                LocalizationService.Get("YesClear"),
                LocalizationService.Get("Cancel"));
            if (clear)
            {
                _cart.Clear();
                _cart.AddItem(RestaurantId, Product, Quantity, deliveryFee: DeliveryFee,
                    variantId: SelectedVariant?.Id, variantName: SelectedVariant?.Name,
                    unitPrice: unitPrice);
            }
            else return;
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task CloseAsync() => await Shell.Current.GoToAsync("..");

    // ✅ FIX: يفتح صورة المنتج بحجمها الكامل مع إمكانية الزوم بإصبعين
    [RelayCommand]
    async Task OpenImage()
    {
        if (string.IsNullOrWhiteSpace(Product?.FullImageUrl)) return;
        var url = Uri.EscapeDataString(Product.FullImageUrl);
        await Shell.Current.GoToAsync($"ImageZoomPage?url={url}");
    }
}