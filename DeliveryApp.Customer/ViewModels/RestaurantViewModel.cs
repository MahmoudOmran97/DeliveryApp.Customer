// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / RestaurantViewModel.cs
// ═══════════════════════════════════════════════════════════════
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Helpers;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(RestaurantId), "id")]
public partial class RestaurantViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly CartService _cart;
    readonly LocationService _location;

    [ObservableProperty] int _restaurantId;
    [ObservableProperty] Restaurant? _restaurant;
    [ObservableProperty] int _cartCount;
    [ObservableProperty] bool _isPharmacy;
    [ObservableProperty] string? _prescriptionPreview;
    [ObservableProperty] string _prescriptionNotes = "";
    [ObservableProperty] bool _hasActivePrescriptionChat;

    // ✅ FEATURE: بحث محلي (client-side) داخل قائمة المحل — بيفلتر MenuGroups
    // بالاسم من غير أي نداء API جديد، لأن القائمة كلها أصلاً محمّلة في Menu.
    [ObservableProperty] string _searchQuery = "";
    [ObservableProperty] bool _isSearchActive;

    partial void OnSearchQueryChanged(string value) => ApplyMenuFilter();

    [RelayCommand]
    void ToggleSearch()
    {
        IsSearchActive = !IsSearchActive;
        if (!IsSearchActive) SearchQuery = "";
    }

    void ApplyMenuFilter()
    {
        var q = SearchQuery?.Trim();

        // تصميم المطعم: بيفلتر MenuGroups (كاتيجوري + منتجاتها) زي ما كان
        MenuGroups.Clear();
        foreach (var c in Menu)
        {
            var items = string.IsNullOrEmpty(q)
                ? c.Products
                : c.Products.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            if (items.Count > 0)
                MenuGroups.Add(new Grouping<Category, Product>(c, items));
        }

        // ✅ FEATURE: نفس البحث دلوقتي شغال لتصميم السوبر ماركت/الصيدلية (شبكة
        // الأقسام) كمان — بيفلتر الأقسام اللي اسمها أو اسم أي منتج جواها بيطابق
        // كلمة البحث، عشان اليوزر يقدر يدور على قسم أو منتج من غير ما يفتح كل قسم لوحده.
        FilteredMenu.Clear();
        foreach (var c in Menu)
        {
            var matches = string.IsNullOrEmpty(q)
                || c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || c.Products.Any(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
            if (matches)
                FilteredMenu.Add(c);
        }
    }

    // ✅ FEATURE: تصميم الصفحة بيتفرّع حسب نوع المحل — المطاعم فاضلة زي ما هي
    // (Menu/MenuGroups تحت بعض)، أما السوبر ماركت/الصيدلية فبتاخد شبكة أقسام
    // (CategoryGrid) + شريط "الأفضل مبيعًا"، والدوس على أي قسم يودّي لصفحة
    // منتجات القسم لوحده (StoreCategoryProductsPage).
    public bool IsGroceryStoreLayout => Restaurant?.IsGroceryStoreLayout == true;

    /// <summary>أعلى المنتجات مبيعًا في المحل كله (IsBestSeller من الـ API) — بتتعرض
    /// كصف أفقي فوق شبكة الأقسام في محلات السوبر ماركت/الصيدلية.</summary>
    public ObservableCollection<Product> BestSellers { get; } = new();

    public ObservableCollection<Category> Menu { get; } = new();

    /// <summary>نسخة مفلترة من Menu بتتفلتر بالبحث (اسم القسم أو اسم أي منتج جواه)
    /// — دي اللي شبكة الأقسام بتاعة السوبر ماركت/الصيدلية بترتبط بيها بدل Menu
    /// مباشرة، عشان زرار البحث يشتغل في التصميم ده كمان.</summary>
    public ObservableCollection<Category> FilteredMenu { get; } = new();

    /// <summary>
    /// نفس الـ Menu بس متلفوفة كـ Grouping عشان CollectionView واحد بس
    /// (IsGrouped=True) يعرضها كلها مع الـ virtualization شغالة صح —
    /// بدل ما كل كاتيجوري كانت بتاخد CollectionView منفصل جوه ScrollView،
    /// وده كان بيخلي كل المنتجات (وصورها) في المطعم كله تترندر مرة واحدة
    /// من أول ما الصفحة تفتح حتى لو مش ظاهرة على الشاشة، وده أكبر سبب للتقل.
    /// </summary>
    public ObservableCollection<Grouping<Category, Product>> MenuGroups { get; } = new();

    public RestaurantViewModel(ApiService api, CartService cart, LocationService location)
    {
        _api = api;
        _cart = cart;
        _location = location;
        _cart.CartChanged += () => { CartCount = _cart.TotalCount; UpdateActivePrescriptionChat(); };
    }

    void UpdateActivePrescriptionChat()
    {
        HasActivePrescriptionChat = RestaurantId != 0
            && _cart.RestaurantId == RestaurantId
            && _cart.PrescriptionRequestId.HasValue;
    }

    [RelayCommand]
    async Task ReturnToPrescriptionChat()
    {
        if (_cart.PrescriptionRequestId is int id)
            await Shell.Current.GoToAsync($"PrescriptionChatPage?requestId={id}");
    }

    partial void OnRestaurantIdChanged(int value) => LoadCommand.Execute(null);

    partial void OnRestaurantChanged(Restaurant? value) => OnPropertyChanged(nameof(IsGroceryStoreLayout));

    [RelayCommand]
    async Task LoadAsync()
    {
        if (RestaurantId == 0) return;
        IsBusy = true;
        try
        {
            // لو معانا موقع العميل، نبعته عشان السيرفر يحسب سعر التوصيل الفعلي حسب المسافة
            var t1 = _location.HasLocation
                ? _api.GetRestaurantAsync(RestaurantId, _location.Latitude, _location.Longitude)
                : _api.GetRestaurantAsync(RestaurantId);
            var t2 = _api.GetMenuAsync(RestaurantId);
            await Task.WhenAll(t1, t2);
            Restaurant = t1.Result;
            IsPharmacy = Restaurant?.StoreType.Equals("Pharmacy", StringComparison.OrdinalIgnoreCase) == true;
            Menu.Clear();
            MenuGroups.Clear();
            FilteredMenu.Clear();
            BestSellers.Clear();
            foreach (var c in t2.Result ?? new())
                Menu.Add(c);
            IsSearchActive = true;
            SearchQuery = "";
            ApplyMenuFilter();

            // ✅ FIX: صف "الأفضل مبيعًا" كان بيتعرض بس لمحلات السوبر ماركت/الصيدلية.
            // دلوقتي بيتعرض لأي نوع محل (مطاعم/أكسسوارات وغيرها) طالما فيه منتجات
            // متعلّمة IsBestSeller — بيتحط آخر حاجة في نهاية صفحة المحل.
            var top = Menu.SelectMany(c => c.Products)
                .Where(p => p.IsBestSeller)
                .OrderByDescending(p => p.SalesCount)
                .Take(10);
            foreach (var p in top) BestSellers.Add(p);

            UpdateActivePrescriptionChat();
        }
        finally { IsBusy = false; }
    }

    // ✅ FEATURE: الدوس على قسم في شبكة أقسام السوبر ماركت/الصيدلية بيودّي
    // لصفحة منتجات القسم ده لوحده (فلترة/فرز + "الأفضل مبيعًا" + "بيتطلب مع")
    [RelayCommand]
    async Task GoToCategory(Category c)
    {
        var fee = (Restaurant?.DeliveryFee ?? 15m).ToString(CultureInfo.InvariantCulture);
        var storeName = Uri.EscapeDataString(Restaurant?.Name ?? "");
        await Shell.Current.GoToAsync(
            $"StoreCategoryProductsPage?restaurantId={RestaurantId}&categoryId={c.Id}&deliveryFee={fee}&storeName={storeName}");
    }

    [RelayCommand]
    async Task ProductTapped(Product p)
    {
        // ✅ FIX: دلوقتي أي منتج (سواء عنده اختيارات أو لأ) بيفتح شاشة التفاصيل
        // (صورة + وصف + سعر + تحكم في الكمية) بدل ما يتضاف للسلة على طول من غير ما
        // المستخدم يشوف حاجة.
        var json = Uri.EscapeDataString(JsonSerializer.Serialize(p));
        // ✅ FIX: نفس مشكلة LocationPickerViewModel — لازم InvariantCulture عشان الفاصلة
        // العشرية تفضل "." مش "٫" لو اللغة عربي، وإلا الـ decimal QueryProperty
        // في ProductOptionsPage بيفشل بـ FormatException.
        var fee = (Restaurant?.DeliveryFee ?? 15m).ToString(CultureInfo.InvariantCulture);
        await Shell.Current.GoToAsync(
            $"ProductOptionsPage?product={json}&restaurantId={RestaurantId}&deliveryFee={fee}");
    }

    [RelayCommand]
    async Task AddToCart(Product p)
    {
        var ok = _cart.AddItem(RestaurantId, p, deliveryFee: Restaurant?.DeliveryFee ?? 15m);
        if (!ok)
        {
            bool clear = await Shell.Current.DisplayAlert(
                LocalizationService.Get("DifferentRestaurant"),
                LocalizationService.Get("DifferentRestaurantMsg"),
                LocalizationService.Get("YesClear"),
                LocalizationService.Get("Cancel"));

            if (clear)
            {
                _cart.Clear();
                _cart.AddItem(RestaurantId, p, deliveryFee: Restaurant?.DeliveryFee ?? 15m);
            }
        }
    }

    [RelayCommand]
    async Task UploadPrescriptionAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = LocalizationService.Get("UploadPrescription"),
                FileTypes = FilePickerFileType.Images
            });
            if (result == null) return;

            IsBusy = true;
            var url = await _api.UploadPrescriptionAsync(result);
            if (string.IsNullOrEmpty(url))
            {
                await AlertAsync(LocalizationService.Get("UploadFailed"));
                return;
            }

            PrescriptionPreview = url.StartsWith("http") ? url : $"https://deliveryappapi.runasp.net{url}";

            // ✅ بدل ما نروح checkout على طول بسعر مجهول، نعمل PrescriptionRequest
            // ونفتح شات مع صاحب الصيدلية عشان يحدد تمن الفاتورة الأول.
            var created = await _api.CreatePrescriptionRequestAsync(RestaurantId, url, PrescriptionNotes);
            if (created == null)
            {
                await AlertAsync(LocalizationService.Get("UploadFailed"));
                return;
            }

            _cart.SetPrescription(RestaurantId, url, PrescriptionNotes, Restaurant?.DeliveryFee ?? 15m);
            _cart.SetPrescriptionRequestId(created.Id);
            await Shell.Current.GoToAsync($"PrescriptionChatPage?requestId={created.Id}");
        }
        catch (Exception ex)
        {
            await AlertAsync(ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    static Task OpenCart() => Shell.Current.GoToAsync("CartPage");
}