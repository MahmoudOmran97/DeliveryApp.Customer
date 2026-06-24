using Android.App;
using Firebase.Messaging;
using Plugin.Firebase.CloudMessaging;

namespace DeliveryApp.Customer;

/// <summary>
/// يستلم الـ FCM push notifications لما التطبيق يكون مقفول أو في الـ background
/// Plugin.Firebase بتعمل ده أوتوماتيك — بس لو محتاج custom handling أضفه هنا
/// </summary>
[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        // Plugin.Firebase بتتعامل مع الـ token refresh أوتوماتيك
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);
        // Plugin.Firebase بتعمل الـ notification أوتوماتيك
        // لو عايز custom handling (مثلاً badge، sound خاص) ضيفه هنا
    }
}
