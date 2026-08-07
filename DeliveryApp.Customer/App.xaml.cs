using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
using DeliveryApp.Customer.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    bool _loggedOutDueToDeactivation;
    int? _navigatingIncomingCallOrderId;
    readonly AuthService _auth;
    readonly SignalRService _signalR;
    readonly ApiService _api;

    /// <summary>true لما التطبيق ظاهر قدام المستخدم — عشان نقرر نفتح CallPage ولا شاشة الرنين الخارجية.</summary>
    public static bool IsInForeground { get; private set; }

    public App(SplashPage splash, ChatNotificationService chatNotif, FcmTokenService fcmToken,
        AuthService auth, SignalRService signalR, ApiService api, IServiceProvider services)
    {
        InitializeComponent();
        _ = chatNotif;
        _auth = auth;
        _signalR = signalR;
        _api = api;

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

        signalR.Reconnected += () => _ = JoinActiveOrderGroupsAsync();

        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            if (auth.IsLoggedIn)
            {
                await fcmToken.RegisterAsync();
                await signalR.ConnectAsync(auth.GetToken());
                await JoinActiveOrderGroupsAsync();
            }

            TryNavigatePendingCall();
            TryNavigatePendingNotification();
        });

        signalR.IncomingVoiceCall += (orderId, callerId) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (_navigatingIncomingCallOrderId == orderId) return;
                _navigatingIncomingCallOrderId = orderId;

#if ANDROID
                if (!IsInForeground)
                {
                    try
                    {
                        Platforms.Android.IncomingCallNotificationHelper.Show(
                            Android.App.Application.Context, orderId, "المندوب");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Call] Show incoming UI failed: {ex.Message}");
                        _navigatingIncomingCallOrderId = null;
                    }
                    return;
                }
#endif
                try
                {
                    await Shell.Current.GoToAsync(
                        $"CallPage?orderId={orderId}&otherPartyName={Uri.EscapeDataString("المندوب")}&isIncoming=true");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Call] Navigate failed: {ex.Message}");
                    _navigatingIncomingCallOrderId = null;
                }
            });
        };

        MainPage = splash;
    }

    protected override void OnResume()
    {
        base.OnResume();
        IsInForeground = true;
        TryNavigatePendingCall();
        TryNavigatePendingNotification();

        if (_auth.IsLoggedIn)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                await _signalR.ConnectAsync(_auth.GetToken());
                await JoinActiveOrderGroupsAsync();
            });
        }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        IsInForeground = false;
    }

    void TryNavigatePendingCall()
    {
        var pendingCall = PendingCallNavigation.TakePending();
        if (pendingCall == null) return;

        var (orderId, callerName, autoAccept) = pendingCall.Value;
        var autoAcceptFlag = autoAccept ? "true" : "false";

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync(
                    $"CallPage?orderId={orderId}&otherPartyName={Uri.EscapeDataString(callerName)}&isIncoming=true&autoAccept={autoAcceptFlag}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Call] Pending navigate failed: {ex.Message}");
            }
        });
    }

    // ✅ NAV FIX — لو المستخدم فتح التطبيق من نوتيفيكيشن عادي (مش مكالمة)، وجّهه
    // للمكان المناسب حسب نوع النوتيفيكيشن بدل ما يفضل واقف على الهوم بس.
    void TryNavigatePendingNotification()
    {
        var pending = PendingNotificationNavigation.TakePending();
        if (pending == null) return;

        var (orderId, type, actionUrl) = pending.Value;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // لو الأدمن حدد وجهة صريحة (ActionUrl) وقت إرسال الإشعار، بتاخد الأولوية.
                // لو مفيش، نرجع للسلوك القديم المعتمد على type/orderId.
                await NotificationNavigationHelper.NavigateAsync(actionUrl, orderId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notif] Pending navigate failed: {ex.Message}");
            }
        });
    }

    async Task JoinActiveOrderGroupsAsync()
    {
        try
        {
            if (!_signalR.IsConnected) return;
            var result = await _api.GetMyOrdersAsync();
            var active = result?.Data?.Where(o => o.IsActive) ?? Enumerable.Empty<Order>();
            foreach (var order in active)
                await _signalR.JoinOrderAsync(order.Id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Call] JoinActiveOrders failed: {ex.Message}");
        }
    }
}
