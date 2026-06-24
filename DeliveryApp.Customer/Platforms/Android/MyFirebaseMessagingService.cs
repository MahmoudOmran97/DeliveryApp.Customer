using Android.App;
using AndroidX.Core.App;
using Firebase.Messaging;

namespace DeliveryApp.Customer;

[Service(Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MyFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        Android.Util.Log.Debug("FCM", $"Token: {token}");
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);

        // ✅ Log عشان نتأكد إن الـ method بتشتغل
        Android.Util.Log.Debug("FCM", "OnMessageReceived called!");

        var title = message.GetNotification()?.Title ?? "New Notification";
        var body = message.GetNotification()?.Body ?? "";

        // ✅ لو فاضي، جرب من data payload
        if (string.IsNullOrEmpty(title) && message.Data != null)
        {
            title = message.Data.ContainsKey("title") ? message.Data["title"] : "New Notification";
            body = message.Data.ContainsKey("body") ? message.Data["body"] : "";
        }

        Android.Util.Log.Debug("FCM", $"Title: {title}, Body: {body}");

        ShowNotification(title, body);
    }

    private void ShowNotification(string title, string body)
    {
        var notificationManager = NotificationManagerCompat.From(this);

        var builder = new NotificationCompat.Builder(this, "default")
            .SetContentTitle(title)
            .SetContentText(body)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetAutoCancel(true)
            .SetDefaults(NotificationCompat.DefaultAll);

        notificationManager.Notify(0, builder.Build());

        Android.Util.Log.Debug("FCM", "Notification shown!");
    }
}