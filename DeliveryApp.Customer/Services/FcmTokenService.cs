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
            System.Diagnostics.Debug.WriteLine($"[FCM] GetTokenAsync returned: {token?.Length ?? 0} chars");

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
            System.Diagnostics.Debug.WriteLine($"[FCM] StackTrace: {ex.StackTrace}");
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
            var title = args.Notification?.Title ?? "";
            var body = args.Notification?.Body ?? "";
            System.Diagnostics.Debug.WriteLine($"[FCM] NotificationReceived: {title} - {body}");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(title, body, "OK");
            });
        };
#endif
    }
}