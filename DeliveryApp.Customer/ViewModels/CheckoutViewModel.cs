using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(Lat), "lat")]
[QueryProperty(nameof(Lng), "lng")]
public partial class CheckoutViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly CartService _cart;
    readonly LocationService _location;
    readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    int _feeRequestVersion = 0;

    [ObservableProperty] string _address = string.Empty;
    [ObservableProperty] bool _useMySavedAddress = true;
    [ObservableProperty] Restaurant? _restaurant;
    [ObservableProperty] string _notes = string.Empty;
    [ObservableProperty] string _paymentMethod = "Cash";
    [ObservableProperty] decimal _subTotal;
    [ObservableProperty] decimal _deliveryFee;
    [ObservableProperty] decimal _total;
    [ObservableProperty] double _deliveryLat = 0;
    [ObservableProperty] double _deliveryLng = 0;
    [ObservableProperty] bool _hasLocationSelected = false;
    [ObservableProperty] string _lat = "";
    [ObservableProperty] string _lng = "";

    // Coupon
    [ObservableProperty] string _couponCode = string.Empty;
    [ObservableProperty] string _couponFeedback = string.Empty;
    [ObservableProperty] bool _hasCouponFeedback;
    [ObservableProperty] bool _couponIsError;
    [ObservableProperty] bool _couponApplied;
    [ObservableProperty] decimal _discount;
    private int? _appliedCouponId;

    public CheckoutViewModel(ApiService api, CartService cart, LocationService location)
    {
        _api = api;
        _cart = cart;
        _location = location;
        SubTotal = cart.TotalPrice;
        DeliveryFee = cart.RestaurantDeliveryFee;
        RecalcTotal();

        if (_location.HasLocation)
        {
            Address = _location.AddressLabel;
            DeliveryLat = _location.Latitude;
            DeliveryLng = _location.Longitude;
        }

        _ = LoadRestaurantAsync();
    }

    async Task LoadRestaurantAsync()
    {
        if (!_cart.RestaurantId.HasValue) return;
        var lat = DeliveryLat == 0 ? (double?)null : DeliveryLat;
        var lng = DeliveryLng == 0 ? (double?)null : DeliveryLng;
        Restaurant = await _api.GetRestaurantAsync(_cart.RestaurantId.Value, lat, lng);
        // ✅ FIX: كان بيفضل ياخد سعر التوصيل الثابت من الكارت (RestaurantDeliveryFee)
        // وميحسبش المسافة الحقيقية، فبدل ما نسيب DeliveryFee زي ما هي نحدّثها من
        // نفس السعر اللي الـ API رجعه (اللي بيحسب المسافة لو بعتنا lat/lng).
        if (Restaurant != null)
            DeliveryFee = Restaurant.DeliveryFee;
    }

    // ── إعادة حساب سعر التوصيل كل ما موقع العميل يتغيّر ──
    // (استخدام موقعي الحالي / اختيار من الخريطة / التبديل لعنواني المسجل)
    async Task RecalculateDeliveryFeeAsync()
    {
        if (!_cart.RestaurantId.HasValue || DeliveryLat == 0 || DeliveryLng == 0) return;

        var version = ++_feeRequestVersion;
        try
        {
            var r = await _api.GetRestaurantAsync(_cart.RestaurantId.Value, DeliveryLat, DeliveryLng);
            // لو وصل رد لطلب أقدم بعد طلب أحدث، تجاهله عشان مايكتبش فوق القيمة الصح
            if (version != _feeRequestVersion || r == null) return;

            Restaurant = r;
            DeliveryFee = r.DeliveryFee;
        }
        catch
        {
            // تجاهل فشل الشبكة هنا — نسيب آخر سعر معروف بدل ما نوقف الشاشة
        }
    }

    // ── Reverse Geocoding (نفس أسلوب HomeLocationPickerViewModel) ──
    // بيحوّل الإحداثيات لاسم مكان مقروء بدل ما العنوان يفضل فاضي أو أرقام
    async Task ResolveAddressAsync(double lat, double lng)
    {
        try
        {
            var url = string.Format(CultureInfo.InvariantCulture,
                "https://nominatim.openstreetmap.org/reverse?lat={0:F6}&lon={1:F6}&format=json&accept-language=ar",
                lat, lng);

            _httpClient.DefaultRequestHeaders.Remove("User-Agent");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "DeliveryApp/1.0");

            var result = await _httpClient.GetFromJsonAsync<NominatimResponse>(url);
            var addr = result?.Address;
            if (addr == null) { Address = $"{lat:F4}, {lng:F4}"; return; }

            var parts = new List<string>();
            var detail = addr.Suburb ?? addr.Neighbourhood ?? addr.Road ?? addr.Village ?? addr.Town;
            if (!string.IsNullOrEmpty(detail)) parts.Add(detail);
            var city = addr.City ?? addr.County ?? addr.State;
            if (!string.IsNullOrEmpty(city) && city != detail) parts.Add(city);

            Address = parts.Count > 0 ? string.Join("، ", parts) : $"{lat:F4}, {lng:F4}";
        }
        catch
        {
            Address = $"{lat:F4}, {lng:F4}";
        }
    }

    private class NominatimResponse
    {
        [JsonPropertyName("address")] public NominatimAddress? Address { get; set; }
    }

    private class NominatimAddress
    {
        [JsonPropertyName("road")] public string? Road { get; set; }
        [JsonPropertyName("suburb")] public string? Suburb { get; set; }
        [JsonPropertyName("neighbourhood")] public string? Neighbourhood { get; set; }
        [JsonPropertyName("village")] public string? Village { get; set; }
        [JsonPropertyName("town")] public string? Town { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("county")] public string? County { get; set; }
        [JsonPropertyName("state")] public string? State { get; set; }
    }

    partial void OnUseMySavedAddressChanged(bool value)
    {
        if (value && _location.HasLocation)
        {
            Address = _location.AddressLabel;
            DeliveryLat = _location.Latitude;
            DeliveryLng = _location.Longitude;
            _ = RecalculateDeliveryFeeAsync();
        }
        else if (!value)
        {
            Address = string.Empty;
            DeliveryLat = 0;
            DeliveryLng = 0;
            HasLocationSelected = false;
        }
    }

    void RecalcTotal() => Total = SubTotal + DeliveryFee - Discount;

    partial void OnDiscountChanged(decimal v) => RecalcTotal();
    partial void OnDeliveryFeeChanged(decimal v) => RecalcTotal();

    partial void OnLatChanged(string v)
    {
        if (double.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var val))
            DeliveryLat = val;
    }

    partial void OnLngChanged(string v)
    {
        if (double.TryParse(v, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var val))
        {
            DeliveryLng = val;
            // ✅ FIX: العنوان اللي المستخدم مختاره من الخريطة كان بيتجاهل تماماً —
            // مكنش بيتحط في Address ولا بيتحسب على أساسه سعر التوصيل.
            if (DeliveryLat != 0)
            {
                _ = ResolveAddressAsync(DeliveryLat, DeliveryLng);
                _ = RecalculateDeliveryFeeAsync();
            }
        }
    }

    partial void OnDeliveryLatChanged(double v) => HasLocationSelected = v != 0;

    // ── تطبيق كوبون الخصم ──
    [RelayCommand]
    async Task ApplyCouponAsync()
    {
        if (string.IsNullOrWhiteSpace(CouponCode)) return;
        IsBusy = true;
        try
        {
            var result = await _api.ValidateCouponAsync(CouponCode.Trim().ToUpper(), SubTotal);
            _appliedCouponId = result?.Id;
            Discount = result?.Discount ?? 0;
            CouponApplied = true;
            ShowCouponFeedback($"✅ {result?.Title} — {LocalizationService.Get("Discount")}: {Discount:F2} EGP", false);
        }
        catch (ApiService.ApiException ex)
        {
            Discount = 0;
            CouponApplied = false;
            _appliedCouponId = null;
            ShowCouponFeedback(ex.Message, true);
        }
        finally { IsBusy = false; }
    }

    void ShowCouponFeedback(string msg, bool isError)
    {
        CouponFeedback = msg;
        CouponIsError = isError;
        HasCouponFeedback = true;
    }

    // ── استخدام الموقع الحالي ──
    [RelayCommand]
    async Task UseCurrentLocation()
    {
        IsBusy = true;
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert(
                    LocalizationService.Get("Error"),
                    LocalizationService.Get("LocationPermissionDenied"),
                    LocalizationService.Get("Ok"));
                return;
            }

            var loc = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium)
                { Timeout = TimeSpan.FromSeconds(10) });

            if (loc != null)
            {
                DeliveryLat = loc.Latitude;
                DeliveryLng = loc.Longitude;
                // ✅ FIX: كان بيوقف عند تحديد الإحداثيات بس، من غير ما يحدّث نص
                // العنوان ولا يعيد حساب سعر التوصيل بناءً على الموقع الجديد.
                _ = ResolveAddressAsync(DeliveryLat, DeliveryLng);
                _ = RecalculateDeliveryFeeAsync();
            }
        }
        catch
        {
            await Shell.Current.DisplayAlert(
                LocalizationService.Get("Error"),
                LocalizationService.Get("LocationError"),
                LocalizationService.Get("Ok"));
        }
        finally { IsBusy = false; }
    }

    // ── فتح صفحة الخريطة ──
    [RelayCommand]
    async Task PickLocationOnMap() =>
        await Shell.Current.GoToAsync("LocationPickerPage");

    // ── تأكيد الطلب وإرساله للـ API ──
    [RelayCommand]
    async Task PlaceOrder()
    {
        if (string.IsNullOrWhiteSpace(Address))
        {
            await Shell.Current.DisplayAlert(
                LocalizationService.Get("Notice"),
                LocalizationService.Get("AddressRequired"),
                LocalizationService.Get("Ok"));
            return;
        }

        if (DeliveryLat == 0)
            await UseCurrentLocation();

        if (DeliveryLat == 0)
        {
            await Shell.Current.DisplayAlert(
                LocalizationService.Get("Notice"),
                LocalizationService.Get("AddressRequired"),
                LocalizationService.Get("Ok"));
            return;
        }

        if (_cart.IsEmpty || !_cart.RestaurantId.HasValue)
        {
            await Shell.Current.DisplayAlert(
                LocalizationService.Get("Error"),
                LocalizationService.Get("CartEmpty"),
                LocalizationService.Get("Ok"));
            return;
        }

        // ✅ Check distance (10km restriction from store)
        if (Restaurant != null)
        {
            double dist = LocationService.DistanceKm(DeliveryLat, DeliveryLng, Restaurant.Latitude, Restaurant.Longitude);
            if (dist > 10.0)
            {
                await Shell.Current.DisplayAlert(
                    LocalizationService.Get("Notice"),
                    LocalizationService.Current.TwoLetterISOLanguageName == "ar"
                        ? "عذراً، الموقع المختار بعيد جداً عن المحل (أكثر من 10 كم)"
                        : "Sorry, the selected location is too far from the store (more than 10km)",
                    LocalizationService.Get("Ok"));
                return;
            }
        }

        IsBusy = true;
        try
        {
            var order = await _api.PlaceOrderAsync(
                restaurantId: _cart.RestaurantId.Value,
                items: _cart.Items.ToList(),
                address: Address,
                lat: DeliveryLat,
                lng: DeliveryLng,
                notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes,
                paymentMethod: PaymentMethod,
                couponCode: CouponApplied ? CouponCode : null,
                couponId: CouponApplied ? _appliedCouponId : null,
                prescriptionImageUrl: _cart.PrescriptionImageUrl,
                prescriptionNotes: _cart.PrescriptionNotes
            );

            if (order != null)
            {
                _cart.Clear();
                await Shell.Current.GoToAsync("//OrdersPage", animate: true);
                await Task.Delay(300);
                await Shell.Current.GoToAsync($"OrderTrackingPage?orderId={order.Id}");
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    LocalizationService.Get("Error"),
                    LocalizationService.Get("OrderFailed"),
                    LocalizationService.Get("Ok"));
            }
        }
        catch (ApiService.ApiException ex)
        {
            await Shell.Current.DisplayAlert(
                LocalizationService.Get("Error"),
                ex.Message,
                LocalizationService.Get("Ok"));
        }
        catch
        {
            await Shell.Current.DisplayAlert(
                LocalizationService.Get("Error"),
                LocalizationService.Get("OrderFailed"),
                LocalizationService.Get("Ok"));
        }
        finally { IsBusy = false; }
    }
}