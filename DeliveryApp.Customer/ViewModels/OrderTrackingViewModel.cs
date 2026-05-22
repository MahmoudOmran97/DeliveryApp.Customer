using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using DeliveryApp.Customer.Models;

using DeliveryApp.Customer.Services;
using System;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(OrderId), "orderId")]

public partial class OrderTrackingViewModel : BaseViewModel

{

    readonly ApiService _api;

    readonly SignalRService _hub;

    readonly AuthService _auth;

    System.Timers.Timer? _timer;

    [ObservableProperty] int _orderId;

    [ObservableProperty] Order? _order;

    [ObservableProperty] string _statusMsg = "Loading...";

    [ObservableProperty] double _progress;

    [ObservableProperty] double _driverLat;

    [ObservableProperty] double _driverLng;

    [ObservableProperty] bool _hasDriver;

    public event Action? MapUpdated;

    public OrderTrackingViewModel(ApiService api, SignalRService hub, AuthService auth)

    {

        _api = api; _hub = hub; _auth = auth;

        _hub.OrderStatusChanged += (id, s) => { if (id == OrderId) _ = LoadAsync(); };

        _hub.DriverLocationUpdated += (lat, lng) =>

        {

            DriverLat = lat; DriverLng = lng;

            HasDriver = true;

            MapUpdated?.Invoke();

        };

    }

    partial void OnOrderIdChanged(int v) => _ = InitAsync();

    async Task InitAsync()

    {

        await LoadAsync();

        await _hub.ConnectAsync(_auth.GetToken());

        await _hub.JoinOrderAsync(OrderId);

        _timer = new System.Timers.Timer(10_000);

        _timer.Elapsed += (_, _) => _ = LoadAsync();

        _timer.Start();

    }

    [RelayCommand]

    async Task LoadAsync()

    {

        Order = await _api.GetOrderAsync(OrderId);

        if (Order == null) return;

        RefreshStatus();

        if (Order.Driver?.CurrentLatitude.HasValue == true)

        {

            DriverLat = Order.Driver.CurrentLatitude!.Value;

            DriverLng = Order.Driver.CurrentLongitude!.Value;

            HasDriver = true;

            MapUpdated?.Invoke();

        }

    }

    void RefreshStatus() => (StatusMsg, Progress) = Order?.Status switch
    {
        "Pending" => (LocalizationService.Get("Status_Pending"), 0.1),
        "Accepted" => (LocalizationService.Get("Status_Accepted"), 0.3),
        "Preparing" => (LocalizationService.Get("Status_Preparing"), 0.55),
        "ReadyForPickup" => (LocalizationService.Get("Status_ReadyForPickup"), 0.70),
        "OnTheWay" => (LocalizationService.Get("Status_OnTheWay"), 0.88),
        "Delivered" => (LocalizationService.Get("Status_Delivered"), 1.0),
        _ => (Order?.StatusText ?? "", 0)
    };

    public void Cleanup()

    {

        _timer?.Stop(); _timer?.Dispose();

        _ = _hub.LeaveOrderAsync(OrderId);

    }

}

