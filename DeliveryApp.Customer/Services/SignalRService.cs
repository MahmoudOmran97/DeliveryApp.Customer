using Microsoft.AspNetCore.SignalR.Client;

using System.Text.Json;

namespace DeliveryApp.Customer.Services;

public class SignalRService

{

    private HubConnection? _hub;

    // ← غير ده

    private const string HubUrl = "https://deliveryappapi.runasp.net/hubs/tracking";

    public event Action<int, string>? OrderStatusChanged;

    public event Action<double, double>? DriverLocationUpdated;

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

    public async Task DisconnectAsync()

    {

        if (_hub != null) { await _hub.StopAsync(); await _hub.DisposeAsync(); _hub = null; }

    }

}

