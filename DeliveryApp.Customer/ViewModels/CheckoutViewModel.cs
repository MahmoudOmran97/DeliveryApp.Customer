using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Services;
using static DeliveryApp.Customer.Services.ApiService;

namespace DeliveryApp.Customer.ViewModels;

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

    // ← الإحداثيات تيجي من الخريطة في الـ View
    public double DeliveryLat { get; set; } = 0;
    public double DeliveryLng { get; set; } = 0;

    public string[] PaymentMethods { get; } = { "Cash", "Card", "Wallet" };

    public CheckoutViewModel(ApiService api, CartService cart)
    {
        _api = api;
        _cart = cart;
        SubTotal = cart.TotalPrice;
        Total = SubTotal + DeliveryFee;
    }

    partial void OnDeliveryFeeChanged(decimal v) => Total = SubTotal + v;

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