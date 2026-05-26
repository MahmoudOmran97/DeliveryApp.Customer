using DeliveryApp.Customer.Models;

namespace DeliveryApp.Customer.Services;

/// <summary>
/// يستمع لرسائل الشات من SignalR ولو العميل مش على شاشة الشات،
/// يوريله notification بانر داخل التطبيق ويروح لصفحة الشات.
/// </summary>
public class ChatNotificationService
{
    private readonly SignalRService _signalR;
    private readonly AuthService _auth;

    // orderId → driverName  (بيتبنى من بيانات الطلب النشط)
    private readonly Dictionary<int, string> _activeOrderDrivers = new();

    // orderId → هل العميل على صفحة الشات دلوقتي؟
    public int? ActiveChatOrderId { get; set; }

    public event Action<ChatNotification>? NewMessageFromDriver;

    public ChatNotificationService(SignalRService signalR, AuthService auth)
    {
        _signalR = signalR;
        _auth = auth;

        _signalR.ChatMessageReceived += OnChatMessageReceived;
    }

    /// <summary>
    /// سجّل orderId مع اسم الدرايفر عشان تعرف تكتب إيه في الـ notification.
    /// استدعيه من OrderTrackingViewModel لما يجيب بيانات الطلب.
    /// </summary>
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
        // لو العميل نفسه بعت الرسالة، متعملش notification
        var myId = _auth.GetUserId().ToString();
        if (senderId == myId) return;

        // لو العميل فاتح صفحة الشات لنفس الطلب ده، متعملش notification
        if (ActiveChatOrderId == orderId) return;

        var driverName = _activeOrderDrivers.TryGetValue(orderId, out var n) ? n : "المندوب";

        var notification = new ChatNotification
        {
            OrderId = orderId,
            DriverName = driverName,
            LastMessage = message.Length > 40 ? message[..40] + "..." : message
        };

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // إطلاع الـ observers (ممكن تربطه بـ Toast أو InApp Banner)
            NewMessageFromDriver?.Invoke(notification);

            // عرض notification بسيطة داخل التطبيق وتسأل العميل يفتح الشات
            await ShowInAppChatAlertAsync(notification);
        });
    }

    private static async Task ShowInAppChatAlertAsync(ChatNotification n)
    {
        try
        {
            var page = Shell.Current as Page ?? Application.Current?.MainPage;
            if (page == null) return;

            var go = await page.DisplayAlert(
                $"💬 رسالة من {n.DriverName}",
                n.LastMessage,
                "فتح المحادثة",
                "لاحقاً");

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