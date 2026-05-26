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

    [ObservableProperty] private int _orderId;
    [ObservableProperty] private string _driverName = string.Empty;
    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private bool _isConnected;

    public ObservableCollection<DriverChatMessage> Messages { get; } = new();

    public DriverChatViewModel(SignalRService signalR, AuthService auth)
    {
        _signalR = signalR;
        _auth = auth;

        _signalR.ChatMessageReceived += OnChatMessageReceived;
    }

    partial void OnOrderIdChanged(int value)
    {
        if (value > 0)
            _ = EnsureConnectedAsync();
    }

    private async Task EnsureConnectedAsync()
    {
        if (!_signalR.IsConnected)
        {
            await _signalR.ConnectAsync(_auth.GetToken());
        }
        // الانضمام لغرفة الطلب عشان يستقبل الرسائل
        await _signalR.JoinOrderAsync(OrderId);
        IsConnected = _signalR.IsConnected;
    }

    private void OnChatMessageReceived(int orderId, string senderId, string message)
    {
        if (orderId != OrderId) return;

        // الرسالة من الدرايفر مش من العميل نفسه
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

        Messages.Add(new DriverChatMessage
        {
            Text = text,
            IsFromMe = true,
            Timestamp = DateTime.Now
        });

        await _signalR.SendChatMessageAsync(OrderId, text);
    }

    public void Cleanup()
    {
        _signalR.ChatMessageReceived -= OnChatMessageReceived;
    }
}