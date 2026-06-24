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
            await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
            var token = await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return;
            System.Diagnostics.Debug.WriteLine($"[FCM] Token: ...{token[^10..]}");
            await _api.UpdateFcmTokenAsync(token);
            await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.SubscribeToTopicAsync("all");
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
            // عرض alert لما التطبيق شغال (Foreground)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Shell.Current != null)
                    await Shell.Current.DisplayAlert(title, body, "OK");
            });
        };
#endif
    }
}