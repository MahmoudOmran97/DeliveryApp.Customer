using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;
using static DeliveryApp.Customer.Services.ApiService;
using Microsoft.Maui.ApplicationModel;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(Lat), "lat")]
[QueryProperty(nameof(Lng), "lng")]
public partial class CheckoutViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly CartService _cart;

    [ObservableProperty] string _address = string.Empty;
    [ObservableProperty] string _notes = string.Empty;
    [ObservableProperty] string _paymentMethod = "Cash";
    [ObservableProperty] decimal _subTotal;
    [ObservableProperty] decimal _deliveryFee = 15;
    [ObservableProperty] decimal _total;

    // ← الإحداثيات تيجي من الخريطة في الـ View أو من الـ GPS
    [ObservableProperty] double _deliveryLat = 0;
    [ObservableProperty] double _deliveryLng = 0;
    [ObservableProperty] bool _hasLocationSelected = false;
    [ObservableProperty] string _locationButtonText = string.Empty;

    // متغيرات لاستقبال البيانات من صفحة الخريطة
    [ObservableProperty] string _lat = "";
    [ObservableProperty] string _lng = "";

    public string[] PaymentMethods { get; } = { "Cash", "Card", "Wallet" };

    public CheckoutViewModel(ApiService api, CartService cart)
    {
        _api = api;
        _cart = cart;
        SubTotal = cart.TotalPrice;
        Total = SubTotal + DeliveryFee;
        UpdateLocationButtonText();
    }

    partial void OnDeliveryFeeChanged(decimal v) => Total = SubTotal + v;

    // تحديث الإحداثيات عند استقبالها من صفحة الخريطة
    partial void OnLatChanged(string v)
    {
        if (double.TryParse(v, out var val)) DeliveryLat = val;
    }

    partial void OnLngChanged(string v)
    {
        if (double.TryParse(v, out var val)) DeliveryLng = val;
    }

    // تحديث حالة الزر عند تغير الإحداثيات
    partial void OnDeliveryLatChanged(double v) => UpdateLocationButtonText();
    partial void OnDeliveryLngChanged(double v) => UpdateLocationButtonText();

    void UpdateLocationButtonText()
    {
        if (DeliveryLat != 0 && DeliveryLng != 0)
        {
            HasLocationSelected = true;
            LocationButtonText = $"✓ {DeliveryLat:F4}, {DeliveryLng:F4}";
        }
        else
        {
            HasLocationSelected = false;
            LocationButtonText = LocalizationService.Get("PickAddressOnMap");
        }
    }

    [RelayCommand]
    async Task UseCurrentLocation()
    {
        IsBusy = true;
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                var req = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
                var loc = await Geolocation.Default.GetLocationAsync(req);
                if (loc != null)
                {
                    DeliveryLat = loc.Latitude;
                    DeliveryLng = loc.Longitude;
                    await AlertAsync($"Location: {DeliveryLat:F4}, {DeliveryLng:F4}");
                }
                else
                    await AlertAsync(LocalizationService.Get("LocationError"));
            }
            else
                await AlertAsync(LocalizationService.Get("LocationPermissionDenied"));
        }
        catch (Exception ex) { await AlertAsync($"Error: {ex.Message}"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    async Task PickLocationOnMap()
    {
        await Shell.Current.GoToAsync("LocationPickerPage");
    }

    [RelayCommand]
    async Task PlaceOrder()
    {
        if (string.IsNullOrWhiteSpace(Address))
        { await AlertAsync("Please enter your delivery address"); return; }

        // لو الـ View لم يحدد موقع على الخريطة نجيبه من الـ GPS مباشرة
        if (DeliveryLat == 0)
        {
            try
            {
                var req = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
                var loc = await Geolocation.Default.GetLocationAsync(req);
                if (loc != null) { DeliveryLat = loc.Latitude; DeliveryLng = loc.Longitude; }
            }
            catch { /* fallback للقاهرة */ DeliveryLat = 30.0444; DeliveryLng = 31.2357; }
        }

        IsBusy = true;
        try
        {
            var order = await _api.PlaceOrderAsync(
                _cart.RestaurantId!.Value, _cart.Items.ToList(),
                Address, DeliveryLat, DeliveryLng, Notes, PaymentMethod);

            if (order != null)
            {
                _cart.Clear();
                await Shell.Current.GoToAsync($"//OrdersPage");
                await Shell.Current.GoToAsync($"OrderTrackingPage?orderId={order.Id}");
            }
            else
                await AlertAsync("Failed to place order. Please try again.");
        }
        catch (ApiException ex) { await AlertAsync(ex.Message); }
        finally { IsBusy = false; }
    }
}
