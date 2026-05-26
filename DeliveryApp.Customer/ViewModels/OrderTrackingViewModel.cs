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

    readonly ChatNotificationService _chatNotif;

    System.Timers.Timer? _timer;

    [ObservableProperty] int _orderId;

    [ObservableProperty] Order? _order;

    [ObservableProperty] string _statusMsg = "Loading...";

    [ObservableProperty] double _progress;

    [ObservableProperty] double _driverLat;

    [ObservableProperty] double _driverLng;

    [ObservableProperty] bool _hasDriver;

    // موقع العميل (وجهة التوصيل)
    [ObservableProperty] double _customerLat;
    [ObservableProperty] double _customerLng;
    [ObservableProperty] string _travelTime = "0 min";
    [ObservableProperty] string _distance = "0 km";
    // موقع المطعم
    [ObservableProperty] double _restaurantLat;
    [ObservableProperty] double _restaurantLng;

    public event Action? MapUpdated;

    public OrderTrackingViewModel(ApiService api, SignalRService hub, AuthService auth, ChatNotificationService chatNotif)

    {

        _api = api; _hub = hub; _auth = auth; _chatNotif = chatNotif;

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

        // موقع العميل (وجهة التوصيل)
        if (Order.DeliveryLatitude != 0 && Order.DeliveryLongitude != 0)
        {
            CustomerLat = Order.DeliveryLatitude;
            CustomerLng = Order.DeliveryLongitude;
        }

        // موقع المطعم
        if (Order.Restaurant != null && Order.Restaurant.Latitude != 0)
        {
            RestaurantLat = Order.Restaurant.Latitude;
            RestaurantLng = Order.Restaurant.Longitude;
        }

        // بعث MapUpdated عشان تتعمل الـ pins والـ route
        MapUpdated?.Invoke();

        if (Order.Driver?.CurrentLatitude.HasValue == true)

        {

            DriverLat = Order.Driver.CurrentLatitude!.Value;

            DriverLng = Order.Driver.CurrentLongitude!.Value;

            HasDriver = true;

            MapUpdated?.Invoke();

        }

        // سجّل الطلب مع ChatNotificationService لما يجي chat notification
        if (Order.Driver != null)
            _chatNotif.RegisterOrder(Order.Id, Order.Driver.Name);

    }

    [RelayCommand]
    async Task OpenChatAsync()
    {
        var driverName = Order?.Driver?.Name ?? "المندوب";
        await Shell.Current.GoToAsync(
            $"DriverChatPage?orderId={OrderId}&driverName={Uri.EscapeDataString(driverName)}");
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
        _timer?.Stop();
        _timer?.Dispose();

        // BUG FIX: We should NOT leave the SignalR group here if we are navigating to the Chat page.
        // If we leave, the customer won't receive messages in the chat page because the SignalR connection is shared.
        // Instead, we let the ChatPage handle its own connection/cleanup or keep the tracking group active.
        // For now, let's keep the group active so notifications and chat work.
        // _ = _hub.LeaveOrderAsync(OrderId); 

        _chatNotif.UnregisterOrder(OrderId);
    }

}