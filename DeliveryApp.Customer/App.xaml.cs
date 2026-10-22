using DeliveryApp.Customer.Services;
using DeliveryApp.Customer.Views;
using DeliveryApp.Customer.Models;
using DeliveryApp.Customer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DeliveryApp.Customer;

public partial class App : Application
{
    bool _loggedOutDueToDeactivation;
    int? _navigatingIncomingCallOrderId;
    readonly AuthService _auth;
    readonly SignalRService _signalR;
    readonly ApiService _api;
    readonly LocationService _location;
    (double lat, double lng)? _lastSentLocation;

    public static bool IsInForeground { get; private set; }

    // ✅ PERF FIX: بيسمح لأي عنصر (زي CurvedBottomBar) إنه يوقف أنيميشن لوب
    // بتاعه لما التطبيق يروح الخلفية بدل ما يفضل شغال ويستهلك بطارية/CPU من
    // غير داعي، ويرجعه لما التطبيق يرجع للـ foreground.
    public static event Action<bool>? ForegroundChanged;

    public App(SplashPage splash, ChatNotificationService chatNotif, FcmTokenService fcmToken,
        AuthService auth, SignalRService signalR, ApiService api, IServiceProvider services)
    {
        InitializeComponent();
        _auth = auth;
        _signalR = signalR;
        _api = api;

        // 📍 بنبعت موقع التوصيل للسيرفر (للإشعارات حسب المنطقة) كل ما يتغير
        _location = services.GetRequiredService<LocationService>();
        _location.LocationChanged += () => _ = SyncLocationAsync();

        async Task HandleAccountDeactivatedAsync()
        {
            if (_loggedOutDueToDeactivation) return;
            _loggedOutDueToDeactivation = true;

            try { await signalR.DisconnectAsync(); } catch { }
            auth.Logout();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var loginPage = services.GetRequiredService<LoginPage>();
                MainPage = new NavigationPage(loginPage);
                await MainPage.DisplayAlert(
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
            var initTask = auth.InitializeAsync();
            await Task.Delay(1500);
            // ✅ تأكيد إن كاش التوكن (من SecureStorage) خلص تحميل قبل ما نشيك
            // IsLoggedIn هنا — أول نداء لـ InitializeAsync() بيتحمّل فعليًا، أي
            // نداء بعد كده (زي اللي في SplashPage) بياخد نفس الـ Task المحفوظ.
            await initTask;
            if (auth.IsLoggedIn)
            {
                await fcmToken.RegisterAsync();
                await SyncLocationAsync();
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

    // Helper for SplashPage to set Shell/Login
    public void SetMainPage(Page page) => MainPage = page;

    protected override void OnResume()
    {
        base.OnResume();
        IsInForeground = true;
        ForegroundChanged?.Invoke(true);
        TryNavigatePendingCall();
        TryNavigatePendingNotification();

        if (_auth.IsLoggedIn)
        {
            _ = Task.Run(async () =>
            {
                await _auth.InitializeAsync();
                await Task.Delay(500);
                await SyncLocationAsync();
                await _signalR.ConnectAsync(_auth.GetToken());
                await JoinActiveOrderGroupsAsync();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (Shell.Current?.CurrentPage?.BindingContext is HomeViewModel homeVm)
                            await homeVm.RefreshNotificationsCountAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Notifications] Resume refresh failed: {ex.Message}");
                    }
                });
            });
        }
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        IsInForeground = false;
        ForegroundChanged?.Invoke(false);
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

    /// <summary>
    /// بيوجّه المستخدم لمكان الإشعار اللي ضغط عليه (actionUrl / orderId).
    /// ✅ FIX: كان بيستهلك الإشعار المعلّق (TakePending) حتى لو التطبيق لسه على شاشة
    /// الـ Splash/Login (Shell مش جاهز) فالتوجيه كان بيفشل ويضيع → التطبيق يفتح بس.
    /// دلوقتي لو الـ Shell مش جاهز بنسيب الإشعار معلّق، وAppShell.Loaded بينادي الدالة دي
    /// تاني أول ما الـ Shell يظهر.
    /// </summary>
    public void TryNavigatePendingNotification()
    {
        if (!PendingNotificationNavigation.HasPending) return;
        if (MainPage is not Shell) return;   // لسه Splash/Login/Onboarding

        var pending = PendingNotificationNavigation.TakePending();
        if (pending == null) return;

        var (orderId, type, actionUrl) = pending.Value;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // مهلة صغيرة عشان الـ Shell يخلّص رسم أول صفحة قبل الـ GoToAsync
                await Task.Delay(350);
                await NotificationNavigationHelper.NavigateAsync(actionUrl, orderId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Notif] Pending navigate failed: {ex.Message}");
            }
        });
    }

    // بيبعت آخر موقع توصيل اختاره العميل (لو متغيّر عن آخر مرة بعتناه)
    async Task SyncLocationAsync()
    {
        try
        {
            if (!_auth.IsLoggedIn || !_location.HasLocation) return;

            var lat = _location.Latitude;
            var lng = _location.Longitude;
            if (_lastSentLocation is { } last
                && Math.Abs(last.lat - lat) < 0.0001 && Math.Abs(last.lng - lng) < 0.0001)
                return;

            await _api.UpdateMyLocationAsync(lat, lng);
            _lastSentLocation = (lat, lng);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Location] sync failed: {ex.Message}");
        }
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
