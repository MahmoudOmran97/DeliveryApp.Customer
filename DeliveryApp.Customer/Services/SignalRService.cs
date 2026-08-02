// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / Services / SignalRService.cs
// ═══════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

namespace DeliveryApp.Customer.Services;

public class SignalRService
{
    private HubConnection? _hub;
    private const string HubUrl = "https://deliveryappapi.runasp.net/hubs/tracking";

    public event Action<int, string>? OrderStatusChanged;
    public event Action<double, double>? DriverLocationUpdated;
    public event Action<int, string, string>? ChatMessageReceived;   // orderId, senderId, message
    public event Action<int, int>? IncomingVoiceCall;
    public event Action<int, int>? VoiceCallAccepted; // orderId, byUserId

    public event Action<int, int>? VoiceCallRejected; // orderId, byUserId
    public event Action<int, int>? VoiceCallEnded;    // orderId, byUserId
    // ✅ FIX #1 & #3 — استقبال إشعار قبول الدرايفر للطلب
    public event Action<int, int, string>? DriverAssigned;          // orderId, driverId, driverName

    // ✅ بيتبعت لما الأدمن يوقف حساب العميل — لازم الأبليكيشن يعمل logout فوري
    public event Action? AccountDeactivated;

    // بعد AutomaticReconnect الجروبات بتتفقد — ننبّه App ترجع تنضم للأوردرات النشطة
    public event Action? Reconnected;

    // ✅ شات الروشتة قبل الأوردر (رسائل مباشرة عن طريق NotifyUserDirectly — مش group)
    public event Action<int, string, string, DateTime>? PrescriptionMessageReceived; // requestId, senderRole, message, createdAt
    public event Action<int, decimal>? PrescriptionPriceSet; // requestId, agreedPrice

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string token)
    {
        if (_hub != null && _hub.State != HubConnectionState.Disconnected)
        {
            if (IsConnected) return;
        }

        if (_hub != null)
        {
            try { await _hub.DisposeAsync(); } catch { }
            _hub = null;
        }

        _hub = new HubConnectionBuilder()
            .WithUrl(HubUrl, o => o.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _hub.Reconnected += _ =>
        {
            Reconnected?.Invoke();
            return Task.CompletedTask;
        };

        _hub.On<JsonElement>("OrderStatusChanged", el =>
        {
            var id = el.GetProperty("orderId").GetInt32();
            var status = el.GetProperty("status").GetString() ?? "";
            MainThread.BeginInvokeOnMainThread(() => OrderStatusChanged?.Invoke(id, status));
        });

        _hub.On<JsonElement>("DriverLocationUpdated", el =>
        {
            var lat = el.GetProperty("latitude").GetDouble();
            var lng = el.GetProperty("longitude").GetDouble();
            MainThread.BeginInvokeOnMainThread(() => DriverLocationUpdated?.Invoke(lat, lng));
        });

        _hub.On<JsonElement>("ChatMessageReceived", el =>
        {
            var orderId = el.GetProperty("orderId").GetInt32();
            var senderId = el.GetProperty("senderId").GetInt32();
            var message = el.GetProperty("message").GetString() ?? "";
            MainThread.BeginInvokeOnMainThread(
                () => ChatMessageReceived?.Invoke(orderId, senderId.ToString(), message));
        });

        _hub.On<JsonElement>("IncomingVoiceCall", el =>
        {
            var orderId = el.GetProperty("orderId").GetInt32();
            var callerId = el.GetProperty("callerId").GetInt32();
            MainThread.BeginInvokeOnMainThread(() => IncomingVoiceCall?.Invoke(orderId, callerId));
        });







        _hub.On<JsonElement>("VoiceCallAccepted", el =>
        {
            var orderId = el.GetProperty("orderId").GetInt32();
            var byUserId = el.GetProperty("byUserId").GetInt32();
            MainThread.BeginInvokeOnMainThread(() => VoiceCallAccepted?.Invoke(orderId, byUserId));
        });

        _hub.On<JsonElement>("VoiceCallRejected", el =>
        {
            var orderId = el.GetProperty("orderId").GetInt32();
            var byUserId = el.GetProperty("byUserId").GetInt32();
            MainThread.BeginInvokeOnMainThread(() => VoiceCallRejected?.Invoke(orderId, byUserId));
        });

        _hub.On<JsonElement>("VoiceCallEnded", el =>
        {
            var orderId = el.GetProperty("orderId").GetInt32();
            var byUserId = el.GetProperty("byUserId").GetInt32();
            MainThread.BeginInvokeOnMainThread(() => VoiceCallEnded?.Invoke(orderId, byUserId));
        });

        // ✅ لما الأدمن يوقف/يفعّل الحساب، السيرفر بيبعت الحالة الجديدة فوراً
        _hub.On<JsonElement>("AccountStatusChanged", el =>
        {
            var isActive = el.TryGetProperty("isActive", out var ia) && ia.GetBoolean();
            if (!isActive)
                MainThread.BeginInvokeOnMainThread(() => AccountDeactivated?.Invoke());
        });

        // ✅ FIX #1 & #3 — السيرفر بيبعت DriverAssigned لما الدرايفر يقبل الطلب
        _hub.On<JsonElement>("DriverAssigned", el =>
        {
            var orderId = el.GetProperty("orderId").GetInt32();
            var driverId = el.GetProperty("driverId").GetInt32();
            var driverName = el.TryGetProperty("driverName", out var dn)
                             ? dn.GetString() ?? "" : "";
            MainThread.BeginInvokeOnMainThread(
                () => DriverAssigned?.Invoke(orderId, driverId, driverName));
        });

        // ✅ شات الروشتة قبل الأوردر
        _hub.On<JsonElement>("PrescriptionMessageReceived", el =>
        {
            var reqId = el.GetProperty("id").GetInt32();
            var role = el.GetProperty("senderRole").GetString() ?? "";
            var msg = el.GetProperty("message").GetString() ?? "";
            var createdAt = el.TryGetProperty("createdAt", out var ca) && ca.TryGetDateTime(out var dt) ? dt : DateTime.UtcNow;
            MainThread.BeginInvokeOnMainThread(
                () => PrescriptionMessageReceived?.Invoke(reqId, role, msg, createdAt));
        });

        _hub.On<JsonElement>("PrescriptionPriceSet", el =>
        {
            var reqId = el.GetProperty("id").GetInt32();
            var price = el.GetProperty("agreedPrice").GetDecimal();
            MainThread.BeginInvokeOnMainThread(() => PrescriptionPriceSet?.Invoke(reqId, price));
        });

        try { await _hub.StartAsync(); }
        catch (Exception ex)
        { System.Diagnostics.Debug.WriteLine($"[SignalR] {ex.Message}"); }
    }

    public async Task JoinOrderAsync(int orderId)
    {
        if (IsConnected) await _hub!.InvokeAsync("JoinOrderTracking", orderId);
    }

    public async Task LeaveOrderAsync(int orderId)
    {
        if (IsConnected) await _hub!.InvokeAsync("LeaveOrderTracking", orderId);
    }

    public async Task SendChatMessageAsync(int orderId, string message)
    {
        if (IsConnected) await _hub!.InvokeAsync("SendChatMessage", orderId, message);
    }

    public async Task StartVoiceCallAsync(int orderId)
    {
        if (IsConnected) await _hub!.InvokeAsync("StartVoiceCall", orderId);
    }

    public async Task AcceptVoiceCallAsync(int orderId)
    {
        if (IsConnected) await _hub!.InvokeAsync("AcceptVoiceCall", orderId);
    }



    public async Task RejectVoiceCallAsync(int orderId)
    {
        if (IsConnected) await _hub!.InvokeAsync("RejectVoiceCall", orderId);
    }

    public async Task EndVoiceCallAsync(int orderId)
    {
        if (IsConnected) await _hub!.InvokeAsync("EndVoiceCall", orderId);
    }

    public async Task DisconnectAsync()
    {
        if (_hub != null) { await _hub.StopAsync(); await _hub.DisposeAsync(); _hub = null; }
    }
}