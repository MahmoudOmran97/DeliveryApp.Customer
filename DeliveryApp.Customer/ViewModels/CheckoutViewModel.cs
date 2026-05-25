using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
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
    [ObservableProperty] double _deliveryLat = 0;
    [ObservableProperty] double _deliveryLng = 0;
    [ObservableProperty] bool _hasLocationSelected = false;
    [ObservableProperty] string _lat = "";
    [ObservableProperty] string _lng = "";

    public CheckoutViewModel(ApiService api, CartService cart)
    {
        _api = api;
        _cart = cart;
        SubTotal = cart.TotalPrice;
        Total = SubTotal + DeliveryFee;
    }

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
            DeliveryLng = val;
    }

    partial void OnDeliveryLatChanged(double v) => HasLocationSelected = v != 0;

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
        // التحقق من العنوان
        if (string.IsNullOrWhiteSpace(Address))
        {
            await Shell.Current.DisplayAlert(
                LocalizationService.Get("Notice"),
                LocalizationService.Get("AddressRequired"),
                LocalizationService.Get("Ok"));
            return;
        }

        // لو محددتش موقع، نجيبه من GPS
        if (DeliveryLat == 0)
            await UseCurrentLocation();

        // لو لسه مفيش موقع، نوقف
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
                paymentMethod: PaymentMethod
            );

            if (order != null)
            {
                // مسح السلة بعد نجاح الطلب
                _cart.Clear();

                // الانتقال لصفحة التتبع
                await Shell.Current.GoToAsync(
                    $"//OrdersPage",
                    animate: true);

                // ثم نفتح التتبع
                await Task.Delay(300);
                await Shell.Current.GoToAsync(
                    $"OrderTrackingPage?orderId={order.Id}");
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