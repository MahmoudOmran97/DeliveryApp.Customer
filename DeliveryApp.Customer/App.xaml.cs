using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    public App(SplashPage splash, ChatNotificationService chatNotif, FcmTokenService fcmToken,
        AuthService auth, SignalRService signalR)
    {
        InitializeComponent();
        _ = chatNotif;

        fcmToken.ListenForTokenRefresh();
        fcmToken.ListenForMessages();

        // Register FCM token only when user is already logged in (needs JWT for API)
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            if (auth.IsLoggedIn)
            {
                await fcmToken.RegisterAsync();

                // ✅ CALL FIX — كانت SignalR بتتوصل بس جوه صفحة تتبع الطلب أو الشات،
                // فلو العميل فاتح صفحة تانية (الهوم مثلاً) مكنش هيوصله نداء المكالمة إطلاقاً.
                // دلوقتي بنوصلها من بداية تشغيل الأبليكيشن عشان تشتغل من أي صفحة.
                await signalR.ConnectAsync(auth.GetToken());
            }
        });

        // ✅ CALL FIX — لما مكالمة واردة توصل والأبليكيشن فاتح (foreground/background بس مش
        // مقفول خالص)، افتح شاشة المكالمة تلقائي زي أي تطبيق اتصال. لو الأبليكيشن مقفول
        // تماماً، ده بيتوصل عن طريق الـ FCM data push بدل SignalR (شوف Platforms/Android
        // للـ full-screen notification).
        signalR.IncomingVoiceCall += (orderId, callerId) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync(
                    $"CallPage?orderId={orderId}&otherPartyName={Uri.EscapeDataString("المندوب")}&isIncoming=true");
            });
        };

        MainPage = splash;
    }
}