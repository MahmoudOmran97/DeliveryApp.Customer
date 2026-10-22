#if ANDROID
using DeliveryApp.Customer;
#endif

namespace DeliveryApp.Customer.Services;

public class FcmTokenService
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

    // ✅ بيتبعت لما توصل نوتيفيكيشن الحساب الموقوف عن طريق الـ FCM (يغطي حالة
    // الأبليكيشن في الخلفية أو لسه بيفتح من جديد بعد ما كان مقفول تماماً)
    public event Action? AccountDeactivated;

    public FcmTokenService(ApiService api, AuthService auth)
    {
        _api = api;
        _auth = auth;
    }

    public async Task RegisterAsync()
    {
#if ANDROID || IOS || MACCATALYST
        try
        {
            System.Diagnostics.Debug.WriteLine("[FCM] RegisterAsync started...");

            await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
            System.Diagnostics.Debug.WriteLine("[FCM] CheckIfValidAsync passed");

            var token = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                System.Diagnostics.Debug.WriteLine("[FCM] Token is empty!");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[FCM] Token: ...{token[^10..]}");

            await _api.UpdateFcmTokenAsync(token);
            System.Diagnostics.Debug.WriteLine("[FCM] Token sent to backend");

            await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.SubscribeToTopicAsync("all");
            System.Diagnostics.Debug.WriteLine("[FCM] Subscribed to topic 'all'");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FCM] RegisterAsync failed: {ex.Message}");
        }
#endif
    }

    public void ListenForTokenRefresh()
    {
#if ANDROID || IOS || MACCATALYST
        Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.TokenChanged += async (_, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"[FCM] TokenChanged: ...{args.Token[^10..]}");
            if (!string.IsNullOrEmpty(args.Token))
                await _api.UpdateFcmTokenAsync(args.Token);
        };
#endif
    }

    public void ListenForMessages()
    {
#if ANDROID || IOS || MACCATALYST
        Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.NotificationReceived += (_, args) =>
        {
            var title = args.Notification?.Title ?? "New Notification";
            var body  = args.Notification?.Body  ?? "";
            var data  = args.Notification?.Data;

            System.Diagnostics.Debug.WriteLine($"[FCM] NotificationReceived: {title} - {body}");

            // ✅ الحساب موقوف — لازم logout فوري بدل عرضه كإشعار عادي بس
            if (data != null && data.TryGetValue("type", out var accType) && accType == "AccountDeactivated")
            {
                MainThread.BeginInvokeOnMainThread(() => AccountDeactivated?.Invoke());
                return;
            }

            // ✅ CALL FIX — لو الـ push ده تنبيه مكالمة واردة (type=IncomingCall)، افتح شاشة
            // المكالمة مباشرة بدل ما نعرضه كإشعار عادي بس. ده بيغطي حالة الأبليكيشن في
            // الخلفية (مش مقفول تماماً) حتى لو الـ SignalR كان لسه مش متوصل.
            if (data != null && data.TryGetValue("type", out var type) && type == "IncomingCall"
                && data.TryGetValue("orderId", out var orderIdStr)
                && int.TryParse(orderIdStr, out var orderId))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync(
                            $"CallPage?orderId={orderId}&otherPartyName={Uri.EscapeDataString("المندوب")}&isIncoming=true");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FCM] Navigate to CallPage failed: {ex.Message}");
                    }
                });
                return;
            }

            // ✅ DUPLICATE FIX: كنا هنا بنعرض إشعار نظام (ShowLocalNotification) وفي نفس الوقت
            // Plugin.Firebase بينادي ShowLocalNotificationAction (MainApplication) فيعرض إشعار تاني
            // لنفس الرسالة → إشعارين. وإشعار الـ handler ده مكانش عليه actionUrl/orderId فالضغط عليه
            // كان يفتح التطبيق بس. دلوقتي إشعار النظام بيتعرض من مكان واحد بس (MainApplication)،
            // وهنا بنكتفي بالتنبيه الداخلي لو التطبيق مفتوح.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TryShowInAppAlert(title, body);
            });
        };
#endif
    }

    // ── In-app alert (foreground only) ───────────────────────────────────────
    private static void TryShowInAppAlert(string title, string body)
    {
        try
        {
            // Use fully-qualified MAUI type to avoid ambiguity with Android.App.Application
            var page = Microsoft.Maui.Controls.Application.Current?.MainPage
                       ?? Shell.Current?.CurrentPage;

            if (page != null)
                _ = page.DisplayAlert(title, body, "OK");
            else
                System.Diagnostics.Debug.WriteLine("[FCM] TryShowInAppAlert: no active page");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FCM] TryShowInAppAlert error: {ex.Message}");
        }
    }
}