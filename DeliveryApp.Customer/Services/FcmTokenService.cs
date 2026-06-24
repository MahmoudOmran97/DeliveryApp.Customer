using Microsoft.Extensions.Logging;
using Plugin.Firebase.CloudMessaging;

namespace DeliveryApp.Customer.Services;

/// <summary>
/// بيجيب الـ FCM token من Firebase وبيبعته للـ API
/// اتسجل كـ Singleton وبتشتغل أوتوماتيك بعد اللوجين
/// </summary>
public class FcmTokenService
{
    private readonly ApiService _api;
    private readonly AuthService _auth;
    private readonly ILogger<FcmTokenService> _logger;

    public FcmTokenService(ApiService api, AuthService auth, ILogger<FcmTokenService> logger)
    {
        _api = api;
        _auth = auth;
        _logger = logger;
    }

    public async Task RegisterAsync()
    {
        try
        {
            await CrossFirebaseCloudMessaging.Current.CheckIfValidAsync();
            var token = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();

            if (string.IsNullOrEmpty(token)) return;

            _logger.LogInformation("[FCM] Token: {Token}", token[^10..]);
            await _api.UpdateFcmTokenAsync(token);

            // اشترك في التوبيك العام (اختياري)
            await CrossFirebaseCloudMessaging.Current.SubscribeToTopicAsync("all");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FCM] Failed to register token");
        }
    }

    /// <summary>
    /// بيستمع لتحديثات الـ token (لازم تشتغل مرة واحدة عند startup)
    /// </summary>
    public void ListenForTokenRefresh()
    {
        CrossFirebaseCloudMessaging.Current.TokenChanged += async (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Token))
                await _api.UpdateFcmTokenAsync(args.Token);
        };
    }

    /// <summary>
    /// بيستمع للـ notifications اللي بتيجي وهو التطبيق شغال (Foreground)
    /// </summary>
    public void ListenForMessages()
    {
        CrossFirebaseCloudMessaging.Current.NotificationReceived += (_, args) =>
        {
            var title = args.Notification?.Title ?? "";
            var body = args.Notification?.Body ?? "";

            // عرض Local Notification وهو التطبيق شغال
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert(title, body, "OK");
            });
        };
    }
}
