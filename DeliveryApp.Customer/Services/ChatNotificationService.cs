// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / Services / ChatNotificationService.cs
// ═══════════════════════════════════════════════════════════════
using DeliveryApp.Customer.Models;

namespace DeliveryApp.Customer.Services;

public class ChatNotificationService
{
    private readonly SignalRService _signalR;
    private readonly AuthService _auth;

    private readonly Dictionary<int, string> _activeOrderDrivers = new();

    public int? ActiveChatOrderId { get; set; }

    public event Action<ChatNotification>? NewMessageFromDriver;

    public ChatNotificationService(SignalRService signalR, AuthService auth)
    {
        _signalR = signalR;
        _auth = auth;
        _signalR.ChatMessageReceived += OnChatMessageReceived;
    }

    public void RegisterOrder(int orderId, string driverName)
    {
        _activeOrderDrivers[orderId] = driverName;
    }

    public void UnregisterOrder(int orderId)
    {
        _activeOrderDrivers.Remove(orderId);
    }

    private void OnChatMessageReceived(int orderId, string senderId, string message)
    {
        var myId = _auth.GetUserId().ToString();
        if (senderId == myId) return;
        if (ActiveChatOrderId == orderId) return;

        // ✅ اسم المندوب الافتراضي مترجم
        var driverName = _activeOrderDrivers.TryGetValue(orderId, out var n)
            ? n
            : LocalizationService.Get("Driver");

        var notification = new ChatNotification
        {
            OrderId = orderId,
            DriverName = driverName,
            LastMessage = message.Length > 40 ? message[..40] + "..." : message
        };

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            NewMessageFromDriver?.Invoke(notification);
            await ShowInAppChatAlertAsync(notification);
        });
    }

    private static async Task ShowInAppChatAlertAsync(ChatNotification n)
    {
        try
        {
            var page = Shell.Current as Page ?? Application.Current?.MainPage;
            if (page == null) return;

            // ✅ ترجمة كل نصوص الـ notification dialog
            var title  = $"💬 {LocalizationService.Get("MessageFrom")} {n.DriverName}";
            var open   = LocalizationService.Get("OpenChat");
            var later  = LocalizationService.Get("Later");

            var go = await page.DisplayAlert(title, n.LastMessage, open, later);

            if (go)
            {
                await Shell.Current.GoToAsync(
                    $"DriverChatPage?orderId={n.OrderId}&driverName={Uri.EscapeDataString(n.DriverName)}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatNotif] {ex.Message}");
        }
    }
}
