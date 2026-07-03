// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / OrderTrackingViewModel.cs
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

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

    [ObservableProperty] double _customerLat;
    [ObservableProperty] double _customerLng;
    [ObservableProperty] string _travelTime = "0 min";
    [ObservableProperty] string _distance = "0 km";

    [ObservableProperty] double _restaurantLat;
    [ObservableProperty] double _restaurantLng;

    public event Action? MapUpdated;

    public OrderTrackingViewModel(
        ApiService api,
        SignalRService hub,
        AuthService auth,
        ChatNotificationService chatNotif)
    {
        _api = api; _hub = hub; _auth = auth; _chatNotif = chatNotif;

        _hub.OrderStatusChanged += (id, s) =>
        {
            if (id == OrderId) _ = LoadAsync();
        };

        _hub.DriverLocationUpdated += (lat, lng) =>
        {
            DriverLat = lat;
            DriverLng = lng;
            HasDriver = true;
            MapUpdated?.Invoke();
        };

        _hub.DriverAssigned += (orderId, driverId, driverName) =>
        {
            if (orderId != OrderId) return;
            HasDriver = true;
            _ = LoadAsync();
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

        if (Order.DeliveryLatitude != 0 && Order.DeliveryLongitude != 0)
        {
            CustomerLat = Order.DeliveryLatitude;
            CustomerLng = Order.DeliveryLongitude;
        }

        if (Order.Restaurant != null && Order.Restaurant.Latitude != 0)
        {
            RestaurantLat = Order.Restaurant.Latitude;
            RestaurantLng = Order.Restaurant.Longitude;
        }

        MapUpdated?.Invoke();

        if (Order.Driver?.CurrentLatitude.HasValue == true)
        {
            DriverLat = Order.Driver.CurrentLatitude!.Value;
            DriverLng = Order.Driver.CurrentLongitude!.Value;
            HasDriver = true;
            MapUpdated?.Invoke();
        }
        else if (Order.Driver != null)
        {
            HasDriver = true;
        }

        if (Order.Driver != null)
            // ✅ اسم المندوب الافتراضي مترجم
            _chatNotif.RegisterOrder(Order.Id, Order.Driver.Name);
    }

    [RelayCommand]
    async Task OpenChatAsync()
    {
        // ✅ اسم المندوب الافتراضي مترجم
        var driverName = Order?.Driver?.Name ?? LocalizationService.Get("Driver");
        await Shell.Current.GoToAsync(
            $"DriverChatPage?orderId={OrderId}&driverName={Uri.EscapeDataString(driverName)}");
    }

    // ✅ FIX — زرار الاتصال كان مش متربط بأي Command خالص في الـ XAML
    [RelayCommand]
    async Task CallDriverAsync()
    {
        if (Order?.Driver == null) return;

        if (!_hub.IsConnected)
        {
            await AlertAsync("لا يوجد اتصال بالسيرفر، تأكد من الإنترنت وحاول تاني.");
            return;
        }

        var driverName = Order.Driver.Name ?? LocalizationService.Get("Driver");
        await _hub.StartVoiceCallAsync(OrderId);
        await Shell.Current.GoToAsync(
            $"CallPage?orderId={OrderId}&otherPartyName={Uri.EscapeDataString(driverName)}&isIncoming=false");
    }

    void RefreshStatus() => (StatusMsg, Progress) = Order?.Status switch
    {
        "Pending"        => (LocalizationService.Get("Status_Pending"), 0.10),
        "Accepted"       => (LocalizationService.Get("Status_Accepted"), 0.30),
        "Preparing"      => (LocalizationService.Get("Status_Preparing"), 0.55),
        "ReadyForPickup" => (LocalizationService.Get("Status_ReadyForPickup"), 0.70),
        "OnTheWay"       => (LocalizationService.Get("Status_OnTheWay"), 0.88),
        "Delivered"      => (LocalizationService.Get("Status_Delivered"), 1.00),
        _                => (Order?.StatusText ?? "", 0.00)
    };

    public void Cleanup()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _chatNotif.UnregisterOrder(OrderId);
    }
}
