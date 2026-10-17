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

    // ✅ FEATURE: مجموعات الاختيارات (اختار 2 صوص من 8 ...)
    public ObservableCollection<ProductOptionGroup> OptionGroups { get; } = new();
    public bool HasOptionGroups => OptionGroups.Count > 0;

    IEnumerable<ProductOptionItem> SelectedOptions =>
        OptionGroups.SelectMany(g => g.Items).Where(i => i.IsSelected);

    // زيادة السعر من الاختيارات المختارة (الصوصات العادية = 0)
    public decimal ExtrasTotal => SelectedOptions.Sum(i => i.ExtraPrice);

    // ✅ FIX: المنتج اللي معندوش اختيارات (Variants) مكانش ينفع يتضاف للسلة خالص
    // لأن CanAddToCart كانت بتعتمد بس على SelectedVariant != null.
    public bool HasVariants => Variants.Count > 0;

    // المقاس لو المنتج له مقاسات + كل مجموعة اختيارات وصلت لأقل عدد مطلوب منها
    public bool CanAddToCart =>
        (!HasVariants || SelectedVariant != null) && OptionGroups.All(g => g.IsSatisfied);

    public decimal CurrentUnitPrice =>
        (SelectedVariant?.Price ?? Product?.DiscountedPrice ?? Product?.Price ?? 0m) + ExtrasTotal;

    public string AddButtonText => !CanAddToCart
        ? LocalizationService.Get("CompleteSelections")
        : ExtrasTotal > 0
            ? $"{LocalizationService.Get("AddToCart")} · {CurrentUnitPrice * Quantity:F0} EGP"
            : LocalizationService.Get("AddToCart");

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(AddButtonText));

    void RefreshAddState()
    {
        OnPropertyChanged(nameof(ExtrasTotal));
        OnPropertyChanged(nameof(CurrentUnitPrice));
        OnPropertyChanged(nameof(CanAddToCart));
        OnPropertyChanged(nameof(AddButtonText));
    }

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

        OptionGroups.Clear();
        foreach (var g in Product.OptionGroups.OrderBy(x => x.SortOrder))
        {
            g.SelectedCount = 0;
            foreach (var i in g.Items) { i.IsSelected = false; i.IsDisabled = false; }
            OptionGroups.Add(g);
        }

        OnPropertyChanged(nameof(HasVariants));
        OnPropertyChanged(nameof(HasOptionGroups));
        RefreshAddState();
    }

    partial void OnSelectedVariantChanged(ProductVariant? value) => RefreshAddState();

    // ✅ اختيار/إلغاء اختيار عنصر من مجموعة (صوص مثلاً):
    //   • أكبر عدد = 1  → بيشتغل زي Radio (اختيار جديد بيشيل القديم)
    //   • أكبر عدد > 1  → لما توصل للحد الأقصى باقي الاختيارات بتتقفل لحد ما تشيل واحد
    [RelayCommand]
    void ToggleOption(ProductOptionItem item)
    {
        var group = OptionGroups.FirstOrDefault(g => g.Items.Contains(item));
        if (group == null || item.IsDisabled) return;

        if (item.IsSelected)
        {
            item.IsSelected = false;
        }
        else if (group.MaxSelect == 1)
        {
            foreach (var other in group.Items) other.IsSelected = false;
            item.IsSelected = true;
        }
        else
        {
            if (group.Items.Count(i => i.IsSelected) >= group.MaxSelect) return;
            item.IsSelected = true;
        }

        group.SelectedCount = group.Items.Count(i => i.IsSelected);
        foreach (var i in group.Items)
            i.IsDisabled = group.MaxSelect > 1 && !i.IsSelected && group.SelectedCount >= group.MaxSelect;

        RefreshAddState();
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
        // ✅ السعر = المقاس/المنتج + إضافات الاختيارات (والسيرفر بيعيد الحساب بنفسه عند الطلب)
        var unitPrice = CurrentUnitPrice;

        var optionIds = SelectedOptions.Select(i => i.Id).ToList();
        var optionsText = BuildOptionsText();

        var ok = _cart.AddItem(
            RestaurantId, Product, Quantity,
            deliveryFee: DeliveryFee,
            variantId: SelectedVariant?.Id,
            variantName: SelectedVariant?.Name,
            unitPrice: unitPrice,
            optionIds: optionIds,
            optionsText: optionsText);

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
                    unitPrice: unitPrice, optionIds: optionIds, optionsText: optionsText);
            }
            else return;
        }

        await Shell.Current.GoToAsync("..");
    }

    // نص الاختيارات للعرض في السلة: "اختار صوص: ثوم، باربكيو | إضافات: جبنة (+5 EGP)"
    string? BuildOptionsText()
    {
        var parts = OptionGroups
            .Select(g => (g, picked: g.Items.Where(i => i.IsSelected).OrderBy(i => i.SortOrder).ToList()))
            .Where(x => x.picked.Count > 0)
            .Select(x => $"{x.g.Name}: {string.Join("، ", x.picked.Select(i => i.HasExtra ? $"{i.Name} ({i.PriceText})" : i.Name))}")
            .ToList();
        return parts.Count == 0 ? null : string.Join(" | ", parts);
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