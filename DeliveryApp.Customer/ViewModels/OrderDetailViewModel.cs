using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Models;

using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(OrderId), "orderId")]

public partial class OrderDetailViewModel : BaseViewModel

{

    readonly ApiService _api;

    [ObservableProperty] int _orderId;

    [ObservableProperty] Order? _order;

    [ObservableProperty] bool _showRating;

    [ObservableProperty] int _restaurantRating = 5;

    [ObservableProperty] int _driverRating = 5;

    [ObservableProperty] string _comment = string.Empty;

    public OrderDetailViewModel(ApiService api) { _api = api; }

    partial void OnOrderIdChanged(int v) => LoadCommand.Execute(null);

    [RelayCommand]

    async Task LoadAsync()

    {

        if (OrderId == 0) return;

        IsBusy = true;

        try { Order = await _api.GetOrderAsync(OrderId); }

        finally { IsBusy = false; }

    }

    [RelayCommand]

    async Task Cancel()

    {

        if (Order == null) return;

        var reason = await Shell.Current.DisplayPromptAsync("Cancel Order", "Reason (optional):");

        if (await _api.CancelOrderAsync(Order.Id, reason)) await LoadAsync();

        else await AlertAsync("Could not cancel at this stage");

    }

    [RelayCommand] void Rate() => ShowRating = true;

    [RelayCommand]

    async Task SubmitRating()

    {

        if (Order == null) return;

        if (await _api.RateOrderAsync(Order.Id, RestaurantRating, DriverRating, Comment))

        { ShowRating = false; await AlertAsync("Thank you for your feedback!"); }

    }

}