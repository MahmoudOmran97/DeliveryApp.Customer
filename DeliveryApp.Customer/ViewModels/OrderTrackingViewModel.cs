// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / ViewModels / OrderTrackingViewModel.cs
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;
using Microsoft.Maui.ApplicationModel;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderTrackingViewModel : BaseViewModel
{
    readonly ApiService _api;
    readonly SignalRService _hub;
    readonly AuthService _auth;
    readonly ChatNotificationService _chatNotif;

    System.Timers.Timer? _timer;
    System.Timers.Timer? _countdownTimer;
    DateTime? _prepStartUtc;
    DateTime? _prepTargetUtc;
    DateTime? _deliveryStartUtc;
    DateTime? _deliveryTargetUtc;
    double _deliveryEstimateSeconds = 25 * 60;

    [ObservableProperty] int _orderId;
    [ObservableProperty] Order? _order;
    [ObservableProperty] string _statusMsg = "Loading...";
    [ObservableProperty] double _progress;
    [ObservableProperty] double _driverLat;
    [ObservableProperty] double _driverLng;
    [ObservableProperty] bool _hasDriver;

    [ObservableProperty] bool _isPrepTimerVisible;
    [ObservableProperty] bool _isDeliveryTimerVisible;
    [ObservableProperty] bool _isWaitingDriverVisible;
    [ObservableProperty] string _prepCountdownText = "00:00";
    [ObservableProperty] string _deliveryCountdownText = "00:00";
    [ObservableProperty] string _prepTimerHint = string.Empty;
    [ObservableProperty] string _deliveryTimerHint = string.Empty;
    [ObservableProperty] string _waitingDriverText = string.Empty;
    [ObservableProperty] double _prepTimerProgress;
    [ObservableProperty] double _deliveryTimerProgress;

    public bool HasCountdownPanel => IsPrepTimerVisible || IsDeliveryTimerVisible || IsWaitingDriverVisible;

    partial void OnIsPrepTimerVisibleChanged(bool value) => OnPropertyChanged(nameof(HasCountdownPanel));
    partial void OnIsDeliveryTimerVisibleChanged(bool value) => OnPropertyChanged(nameof(HasCountdownPanel));
    partial void OnIsWaitingDriverVisibleChanged(bool value) => OnPropertyChanged(nameof(HasCountdownPanel));

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

        _countdownTimer = new System.Timers.Timer(1_000);
        _countdownTimer.Elapsed += (_, _) => MainThread.BeginInvokeOnMainThread(UpdateCountdowns);
        _countdownTimer.Start();
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        Order = await _api.GetOrderAsync(OrderId);
        if (Order == null) return;

        RefreshStatus();
        ConfigureCountdown();
        UpdateCountdowns();

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

    // ── إلغاء الطلب (متاح بس قبل ما يبدأ التحضير — Pending/Accepted) ──
    [RelayCommand]
    async Task CancelOrderAsync()
    {
        if (Order == null || !Order.CanCancel) return;

        var confirm = await Shell.Current.DisplayAlert(
            LocalizationService.Get("CancelOrder"),
            LocalizationService.Get("CancelOrderConfirm"),
            LocalizationService.Get("Ok"),
            LocalizationService.Get("Cancel"));
        if (!confirm) return;

        var reason = await Shell.Current.DisplayPromptAsync(
            LocalizationService.Get("CancelOrder"),
            LocalizationService.Get("CancelReason"));

        if (await _api.CancelOrderAsync(OrderId, reason))
            await LoadAsync();
        else
            await AlertAsync(LocalizationService.Get("CancelFailed"));
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

    void ConfigureCountdown()
    {
        IsPrepTimerVisible = false;
        IsDeliveryTimerVisible = false;
        IsWaitingDriverVisible = false;

        if (Order == null) return;

        switch (Order.Status)
        {
            case "Accepted":
            case "Preparing":
                _prepStartUtc = Order.AcceptedAt ?? Order.CreatedAt;
                // الأولوية لـ EstimatedDeliveryMax (بيتحسب من وقت المحل + أطول منتج في الأوردر
                // وقت القبول)، ولو مش موجودة (طلب قديم قبل التحديث) نرجع للـ fallback الثابت.
                var prepMinutes = Math.Clamp(Order.EstimatedDeliveryMax ?? Order.EstimatedDelivery ?? 25, 10, 90);
                _prepTargetUtc = _prepStartUtc.Value.AddMinutes(prepMinutes);

                PrepTimerHint = (Order.EstimatedDeliveryMin.HasValue && Order.EstimatedDeliveryMax.HasValue
                                  && Order.EstimatedDeliveryMax > Order.EstimatedDeliveryMin)
                    ? string.Format(LocalizationService.Get("Timer_PreparingRangeHint"),
                                     Order.EstimatedDeliveryMin, Order.EstimatedDeliveryMax)
                    : LocalizationService.Get("Timer_PreparingHint");

                IsPrepTimerVisible = true;
                break;

            case "ReadyForPickup":
                WaitingDriverText = LocalizationService.Get("Timer_WaitingDriver");
                IsWaitingDriverVisible = true;
                break;

            case "OnTheWay":
                // ✅ FIX: Order.PickedUpAt جايه من الـ API متحوّلة بالفعل لتوقيت الجهاز المحلي
                // (UtcDateTimeConverter بيعمل ToLocalTime() تلقائي)، فمينفعش نقارنها بـ DateTime.UtcNow
                // لأن ده كان بيضيف فرق التوقيت المحلي (مثلاً 3 ساعات) على العداد.
                _deliveryStartUtc ??= Order.PickedUpAt ?? DateTime.Now;
                _deliveryEstimateSeconds = Math.Max(10 * 60, (Order.EstimatedDeliveryMax ?? Order.EstimatedDelivery ?? 25) * 60);
                _deliveryTargetUtc ??= _deliveryStartUtc.Value.AddSeconds(_deliveryEstimateSeconds);
                DeliveryTimerHint = LocalizationService.Get("Timer_DeliveryHint");
                IsDeliveryTimerVisible = true;
                break;
        }
    }

    void UpdateCountdowns()
    {
        if (Order == null) return;

        // ✅ FIX: القيم اللي جايه من Order (AcceptedAt/CreatedAt/PickedUpAt) بقت متحوّلة
        // بالفعل لتوقيت الجهاز المحلي عن طريق UtcDateTimeConverter، فلازم نقارنها بـ
        // DateTime.Now مش DateTime.UtcNow، وإلا هيتضاف فرق التوقيت (مثلاً 3 ساعات) على العداد.
        var now = DateTime.Now;
        if (IsPrepTimerVisible && _prepStartUtc.HasValue && _prepTargetUtc.HasValue)
        {
            var total = Math.Max(1, (_prepTargetUtc.Value - _prepStartUtc.Value).TotalSeconds);
            var remaining = Math.Max(0, (_prepTargetUtc.Value - now).TotalSeconds);
            PrepCountdownText = FormatCountdown(remaining);
            PrepTimerProgress = Math.Clamp(1 - remaining / total, 0, 1);
        }

        if (IsDeliveryTimerVisible && _deliveryTargetUtc.HasValue)
        {
            var start = _deliveryStartUtc ?? now;
            var total = Math.Max(1, (_deliveryTargetUtc.Value - start).TotalSeconds);
            var remaining = Math.Max(0, (_deliveryTargetUtc.Value - now).TotalSeconds);
            DeliveryCountdownText = FormatCountdown(remaining);
            DeliveryTimerProgress = Math.Clamp(1 - remaining / total, 0, 1);
        }
    }

    public void UpdateDeliveryEta(double durationSeconds)
    {
        if (durationSeconds <= 0 || Order?.Status != "OnTheWay") return;

        _deliveryEstimateSeconds = Math.Max(60, durationSeconds);
        // ✅ FIX: نفس السبب فوق — Order.PickedUpAt توقيت محلي مش UTC
        _deliveryStartUtc ??= Order.PickedUpAt ?? DateTime.Now;
        _deliveryTargetUtc = _deliveryStartUtc.Value.AddSeconds(_deliveryEstimateSeconds);
        MainThread.BeginInvokeOnMainThread(UpdateCountdowns);
    }

    static string FormatCountdown(double seconds)
    {
        var total = Math.Max(0, (int)Math.Ceiling(seconds));
        var hours = total / 3600;
        var minutes = (total % 3600) / 60;
        var secs = total % 60;
        return hours > 0
            ? $"{hours:00}:{minutes:00}:{secs:00}"
            : $"{minutes:00}:{secs:00}";
    }

    public void Cleanup()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _chatNotif.UnregisterOrder(OrderId);
    }
}
