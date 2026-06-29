#if ANDROID
using DeliveryApp.Customer;
#endif

namespace DeliveryApp.Customer.Services;

public class FcmTokenService
{
    private readonly ApiService _api;
    private readonly AuthService _auth;

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

            System.Diagnostics.Debug.WriteLine($"[FCM] NotificationReceived: {title} - {body}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowLocalNotification(title, body);
                TryShowInAppAlert(title, body);
            });
        };
#endif
    }

    // ── Android-only: show a real system notification bar entry ──────────────
#if ANDROID
    private static int _notifId = 1000;

    private static void ShowLocalNotification(string title, string body)
    {
        try
        {
            var context = Android.App.Application.Context;

            var builder = new AndroidX.Core.App.NotificationCompat.Builder(context, "default")
                .SetContentTitle(title)
                .SetContentText(body)
                .SetPriority(AndroidX.Core.App.NotificationCompat.PriorityHigh)
                .SetDefaults(AndroidX.Core.App.NotificationCompat.DefaultAll)
                .SetAutoCancel(true)
                .SetStyle(new AndroidX.Core.App.NotificationCompat.BigTextStyle().BigText(body));

            NotificationHelper.ApplyBranding(builder, context);

            var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
            if (intent != null)
            {
                intent.SetFlags(Android.Content.ActivityFlags.SingleTop | Android.Content.ActivityFlags.ClearTop);
                var pending = Android.App.PendingIntent.GetActivity(
                    context, 0, intent,
                    Android.App.PendingIntentFlags.UpdateCurrent | Android.App.PendingIntentFlags.Immutable);
                builder.SetContentIntent(pending);
            }

            AndroidX.Core.App.NotificationManagerCompat.From(context).Notify(_notifId++, builder.Build());
            System.Diagnostics.Debug.WriteLine($"[FCM] Local notification shown (id: {_notifId - 1})");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FCM] ShowLocalNotification error: {ex.Message}");
        }
    }
#else
    // iOS / MacCatalyst / Windows — Firebase handles its own display
    private static void ShowLocalNotification(string title, string body)
    {
        System.Diagnostics.Debug.WriteLine($"[FCM] ShowLocalNotification (non-Android): {title}");
    }
#endif

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