using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    bool _loggedOutDueToDeactivation;

    public App(SplashPage splash, ChatNotificationService chatNotif, FcmTokenService fcmToken,
        AuthService auth, SignalRService signalR, ApiService api, IServiceProvider services)
    {
        InitializeComponent();
        _ = chatNotif;

        // ✅ لو الأدمن أوقف حساب العميل، اعمل logout فوري من أي مكان في الأبليكيشن.
        // بنسمع الحدث من مصدرين: SignalR (لما الأبليكيشن يكون فاتح ومتصل)،
        // و ApiService (لو أي طلب API عادي رجع 401 بسبب إن الحساب موقوف).
        //
        // ملاحظة: بنعمل resolve لـ LoginPage من الـ IServiceProvider وقت الحاجة بس (جوه
        // الميثود دي)، مش كباراميتر في الـ constructor. لو اتحقنت كباراميتر مباشر، الـ DI
        // كان بيعمل new للصفحة قبل ما App.xaml نفسه يخلص تحميل الـ ResourceDictionary
        // بتاعه، فأي StaticResource (زي InputBorder) مكانش لسه موجود وقت إنشاء الصفحة
        // → XamlParseException.
        async Task HandleAccountDeactivatedAsync()
        {
            if (_loggedOutDueToDeactivation) return;
            _loggedOutDueToDeactivation = true;

            try { await signalR.DisconnectAsync(); } catch { }
            auth.Logout();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var loginPage = services.GetRequiredService<LoginPage>();
                var nav = new NavigationPage(loginPage);
                MainPage = nav;
                await nav.DisplayAlert(
                    "الحساب موقوف",
                    "تم إيقاف حسابك من قبل الإدارة. تواصل مع الدعم لمزيد من التفاصيل.",
                    "حسنًا");
            });
        }

        signalR.AccountDeactivated += () => _ = HandleAccountDeactivatedAsync();
        api.AccountDeactivated += () => _ = HandleAccountDeactivatedAsync();
        fcmToken.AccountDeactivated += () => _ = HandleAccountDeactivatedAsync();

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

            // ✅ لو التطبيق اتفتح لسه (cold start) بسبب دوس على زرار "قبول" في نوتيفيكيشن
            // مكالمة واردة، انقل المستخدم مباشرة لصفحة المكالمة مع قبول تلقائي.
            var pendingCall = Services.PendingCallNavigation.TakePending();
            if (pendingCall != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync(
                        $"CallPage?orderId={pendingCall.Value.orderId}&otherPartyName={Uri.EscapeDataString(pendingCall.Value.callerName)}&isIncoming=true&autoAccept=true");
                });
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