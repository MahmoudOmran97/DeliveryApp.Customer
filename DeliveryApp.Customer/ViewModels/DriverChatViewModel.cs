using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.ViewModels;

[QueryProperty(nameof(OrderId), "orderId")]
[QueryProperty(nameof(DriverName), "driverName")]
public partial class DriverChatViewModel : BaseViewModel
{
    private readonly SignalRService _signalR;
    private readonly AuthService _auth;
    private readonly ApiService _api;

    [ObservableProperty] private int _orderId;
    [ObservableProperty] private string _driverName = string.Empty;
    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private bool _isConnected;

    // نتعقب آخر رسالة بعتها عشان نتجنب الـ echo من السيرفر
    private string? _lastSentMessage;

    public ObservableCollection<DriverChatMessage> Messages { get; } = new();

    private readonly ChatNotificationService _chatNotif;

    public DriverChatViewModel(SignalRService signalR, AuthService auth, ChatNotificationService chatNotif, ApiService api)
    {
        _signalR = signalR;
        _auth = auth;
        _chatNotif = chatNotif;
        _api = api;

        _signalR.ChatMessageReceived += OnChatMessageReceived;
    }

    partial void OnOrderIdChanged(int value)
    {
        if (value > 0)
        {
            _chatNotif.ActiveChatOrderId = value;
            _ = EnsureConnectedAsync();
        }
    }

    private async Task EnsureConnectedAsync()
    {
        if (!_signalR.IsConnected)
            await _signalR.ConnectAsync(_auth.GetToken());

        // الانضمام لغرفة الطلب عشان يستقبل الرسائل
        // Note: Tracking page also joins this group, but SignalR handles multiple joins gracefully.
        await _signalR.JoinOrderAsync(OrderId);

        IsConnected = _signalR.IsConnected;

        // تحميل الرسائل القديمة
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _api.GetChatMessagesAsync(OrderId);
            if (history != null)
            {
                Messages.Clear();
                foreach (var msg in history)
                {
                    Messages.Add(msg);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatHistory] {ex.Message}");
        }
    }

    private void OnChatMessageReceived(int orderId, string senderId, string message)
    {
        if (orderId != OrderId) return;

        // FIX: تجاهل رسائل العميل نفسه (السيرفر بيبعت الرسالة لكل الـ group بما فيه المرسل)
        var myId = _auth.GetUserId().ToString();
        if (senderId == myId) return;

        Messages.Add(new DriverChatMessage
        {
            Text = message,
            IsFromMe = false,
            Timestamp = DateTime.Now
        });
    }

    [RelayCommand]
    private async Task Send()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;

        InputText = string.Empty;

        // أضف الرسالة محلياً فوراً
        Messages.Add(new DriverChatMessage
        {
            Text = text,
            IsFromMe = true,
            Timestamp = DateTime.Now
        });

        // بعت للسيرفر
        await _signalR.SendChatMessageAsync(OrderId, text);
    }

    public void Cleanup()
    {
        _chatNotif.ActiveChatOrderId = null;
        _signalR.ChatMessageReceived -= OnChatMessageReceived;
    }
}