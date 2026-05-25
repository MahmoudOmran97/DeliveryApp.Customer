using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;
using Microsoft.Maui.ApplicationModel;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(Lat), "lat")]
[QueryProperty(nameof(Lng), "lng")]
public partial class CheckoutViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly CartService _cart;

    [ObservableProperty] string _address = string.Empty;
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
        _api = api; _cart = cart;
        SubTotal = cart.TotalPrice; Total = SubTotal + DeliveryFee;
    }

    partial void OnLatChanged(string v) { if (double.TryParse(v, out var val)) DeliveryLat = val; }
    partial void OnLngChanged(string v) { if (double.TryParse(v, out var val)) DeliveryLng = val; }
    partial void OnDeliveryLatChanged(double v) => HasLocationSelected = v != 0;

    [RelayCommand]
    async Task UseCurrentLocation()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
        {
            var loc = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));
            if (loc != null) { DeliveryLat = loc.Latitude; DeliveryLng = loc.Longitude; }
        }
    }

    [RelayCommand]
    async Task PickLocationOnMap() => await Shell.Current.GoToAsync("LocationPickerPage");

    [RelayCommand]
    async Task PlaceOrder()
    {
        if (DeliveryLat == 0) { /* Fallback to GPS if not selected */ await UseCurrentLocation(); }
        // ... كود إرسال الطلب للـ API
    }
}