using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Services;
using System.Net;
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

    public string[] PaymentMethods { get; } = { "Cash", "Card", "Wallet" };

    public CheckoutViewModel(ApiService api, CartService cart)

    {

        _api = api; _cart = cart;

        SubTotal = cart.TotalPrice;

        Total = SubTotal + DeliveryFee;

    }

    partial void OnDeliveryFeeChanged(decimal v) => Total = SubTotal + v;

    [RelayCommand]
    async Task PlaceOrder()
    {
        if (string.IsNullOrWhiteSpace(Address))
        { await AlertAsync("Please enter your delivery address"); return; }

        IsBusy = true;
        try
        {
            var order = await _api.PlaceOrderAsync(
                _cart.RestaurantId!.Value, _cart.Items.ToList(),
                Address, 30.0444, 31.2357, Notes, PaymentMethod);

            if (order != null)
            {
                _cart.Clear();
                await Shell.Current.GoToAsync($"//OrdersPage");
                await Shell.Current.GoToAsync($"OrderTrackingPage?orderId={order.Id}");
            }
            else
                await AlertAsync("Failed to place order. Please try again.");
        }
        catch (ApiException ex)
        {
            // ← دلوقتي بيطلع السبب الحقيقي: "Restaurant is closed", "Minimum order is X EGP" إلخ
            await AlertAsync(ex.Message);
        }
        finally { IsBusy = false; }
    }

}

