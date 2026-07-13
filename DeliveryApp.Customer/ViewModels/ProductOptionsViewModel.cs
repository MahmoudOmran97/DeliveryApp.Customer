using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(ProductJson), "product")]
[QueryProperty(nameof(RestaurantId), "restaurantId")]
[QueryProperty(nameof(DeliveryFee), "deliveryFee")]
public partial class ProductOptionsViewModel : BaseViewModel
{
    readonly CartService _cart;

    [ObservableProperty] string _productJson = "";
    [ObservableProperty] int _restaurantId;
    [ObservableProperty] decimal _deliveryFee = 15m;
    [ObservableProperty] Product? _product;
    [ObservableProperty] ProductVariant? _selectedVariant;
    [ObservableProperty] int _quantity = 1;

    public ObservableCollection<ProductVariant> Variants { get; } = new();

    public bool CanAddToCart => SelectedVariant != null;
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
        if (Product == null || SelectedVariant == null) return;

        var ok = _cart.AddItem(
            RestaurantId, Product, Quantity,
            deliveryFee: DeliveryFee,
            variantId: SelectedVariant.Id,
            variantName: SelectedVariant.Name,
            unitPrice: SelectedVariant.Price);

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
                    variantId: SelectedVariant.Id, variantName: SelectedVariant.Name,
                    unitPrice: SelectedVariant.Price);
            }
            else return;
        }

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task CloseAsync() => await Shell.Current.GoToAsync("..");
}