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
    // ✅ FIX #1 & #3 — استقبال إشعار قبول الدرايفر للطلب
    public event Action<int, int, string>? DriverAssigned;          // orderId, driverId, driverName

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public async Task ConnectAsync(string token)
    {
        if (IsConnected) return;

        _hub = new HubConnectionBuilder()
            .WithUrl(HubUrl, o => o.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

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

    public async Task DisconnectAsync()
    {
        if (_hub != null) { await _hub.StopAsync(); await _hub.DisposeAsync(); _hub = null; }
    }
}