using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using AndroidX.Core.App;
using DeliveryApp.Customer.Platforms.Android;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.Platforms.Android.Extensions;

namespace DeliveryApp.Customer;

[Application]
public class MainApplication : MauiApplication
{
    private const string ChannelId = "default";
    private static readonly HttpClient ImageHttp = new() { Timeout = TimeSpan.FromSeconds(6) };

    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership) { }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate()
    {
        base.OnCreate();

        // ✅ FIX: كانت الاتنين دول بيتظبطوا في MainActivity.OnCreate بس. لما التطبيق يكون
        // مقفول ويوصل إشعار، النظام بيشغّل الـ Process (Application) من غير ما يفتح الـ Activity،
        // فكان ShowLocalNotificationAction بيبقى null والـ Plugin بيعرض إشعار عادي من غير
        // actionUrl/orderId → الضغط عليه يفتح التطبيق بس. دلوقتي بيتظبطوا هنا وقت إنشاء الـ Application.
        CreateNotificationChannel();
        SetupLocalNotificationAction();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "General Notifications", NotificationImportance.High)
            {
                Description = "Default notification channel",
                LockscreenVisibility = NotificationVisibility.Public
            };
            channel.EnableLights(true);
            channel.EnableVibration(true);

            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }

        // Tell Plugin.Firebase which channel to use
        FirebaseCloudMessagingImplementation.ChannelId = ChannelId;
    }

    private void SetupLocalNotificationAction()
    {
        // ✅ This is the OFFICIAL Plugin.Firebase way to show local notifications.
        // Plugin.Firebase calls this action automatically when a message arrives —
        // both foreground AND background.
        FirebaseCloudMessagingImplementation.ShowLocalNotificationAction = notification =>
        {
            try
            {
                var context = ApplicationContext;

                // ✅ لو النوتيفيكيشن دي مكالمة واردة (type=IncomingCall) اعرضها كنوتيفيكيشن
                // full-screen بزرار قبول أخضر ورفض أحمر بدل النوتيفيكيشن العادي.
                var data = notification.Data;
                if (data != null
                    && data.TryGetValue("type", out var type) && type == "IncomingCall"
                    && data.TryGetValue("orderId", out var orderIdStr)
                    && int.TryParse(orderIdStr, out var orderId))
                {
                    IncomingCallNotificationHelper.Show(context, orderId, "المندوب");
                    return;
                }

                var notifId = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % int.MaxValue);

                var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "");
                if (intent != null)
                {
                    intent.PutExtra(FirebaseCloudMessagingImplementation.IntentKeyFCMNotification,
                        notification.ToBundle());
                    intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

                    // ✅ NAV FIX — type/orderId/actionUrl كـ extras بسيطة على الـ intent
                    // عشان لما المستخدم يدوس على الإشعار (حتى والتطبيق مقفول) MainActivity.HandleIntent
                    // يوجّهه لمكان محدد جوه التطبيق بدل ما يفتح على الهوم بس.
                    if (data != null)
                    {
                        if (data.TryGetValue("type", out var notifType) && !string.IsNullOrEmpty(notifType))
                            intent.PutExtra("Taly_notif_type", notifType);
                        if (data.TryGetValue("orderId", out var notifOrderId) && !string.IsNullOrEmpty(notifOrderId))
                            intent.PutExtra("Taly_notif_order_id", notifOrderId);
                        if (data.TryGetValue("actionUrl", out var notifActionUrl) && !string.IsNullOrEmpty(notifActionUrl))
                            intent.PutExtra("Taly_notif_action_url", notifActionUrl);
                    }
                }

                // ✅ FIX: requestCode كان 0 لكل الإشعارات، فمع FLAG_UPDATE_CURRENT كل إشعار جديد
                // كان بيمسح extras الإشعارات اللي قبله، والضغط على إشعار قديم يوجّه لمكان آخر إشعار.
                // دلوقتي لكل إشعار requestCode مختلف.
                var pendingIntent = intent != null
                    ? PendingIntent.GetActivity(context, notifId, intent,
                        PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)
                    : null;

                var builder = new NotificationCompat.Builder(context, ChannelId)
                    .SetContentTitle(notification.Title)
                    .SetContentText(notification.Body)
                    .SetPriority(NotificationCompat.PriorityHigh)
                    .SetDefaults(NotificationCompat.DefaultAll)
                    .SetAutoCancel(true);

                // 🖼️ لو الإشعار فيه صورة (عرض/إعلان من الأدمن) بنعرضها كبيرة (BigPicture)،
                // ولو التنزيل فشل أو اتأخر بنعرض الإشعار نصي عادي من غير ما نضيّعه.
                Bitmap? picture = null;
                if (data != null && data.TryGetValue("imageUrl", out var imageUrl) && !string.IsNullOrWhiteSpace(imageUrl))
                    picture = DownloadBitmap(imageUrl);

                if (picture != null)
                {
                    builder.SetLargeIcon(picture);
                    builder.SetStyle(new NotificationCompat.BigPictureStyle()
                        .BigPicture(picture)
                        .BigLargeIcon((Bitmap?)null)
                        .SetSummaryText(notification.Body));
                }
                else
                {
                    builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Body));
                }

                NotificationHelper.ApplyBranding(builder, context);

                if (pendingIntent != null)
                    builder.SetContentIntent(pendingIntent);

                NotificationManagerCompat.From(context).Notify(notifId, builder.Build());

                Android.Util.Log.Debug("FCM", $"Local notification shown: {notification.Title}");
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("FCM", $"ShowLocalNotificationAction error: {ex.Message}");
            }
        };
    }

    // بينزّل صورة الإشعار (حد أقصى 6 ثواني) ويصغّرها لو عرضها كبير. بيرجّع null لو فشل.
    private static Bitmap? DownloadBitmap(string url)
    {
        try
        {
            // ممكن تتنادى من الـ main thread لو التطبيق مفتوح → نشغّل التنزيل على thread تاني
            var bytes = Task.Run(() => ImageHttp.GetByteArrayAsync(url)).GetAwaiter().GetResult();
            var bitmap = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
            if (bitmap == null) return null;

            const int maxWidth = 1024;
            if (bitmap.Width > maxWidth)
            {
                var height = (int)(bitmap.Height * (maxWidth / (double)bitmap.Width));
                var scaled = Bitmap.CreateScaledBitmap(bitmap, maxWidth, height, true);
                if (scaled != null) return scaled;
            }
            return bitmap;
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("FCM", $"Notification image download failed: {ex.Message}");
            return null;
        }
    }
}
