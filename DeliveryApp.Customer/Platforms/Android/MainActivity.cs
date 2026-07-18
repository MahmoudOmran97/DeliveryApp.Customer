using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using DeliveryApp.Customer.Platforms.Android;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.Platforms.Android.Extensions;

namespace DeliveryApp.Customer
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
            | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationPermissionRequestCode = 1001;
        private const string ChannelId = "default";

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleIntent(Intent);
            CreateNotificationChannel();
            SetupLocalNotificationAction();
            RequestNotificationPermissionIfNeeded();
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        private static void HandleIntent(Intent? intent)
        {
            if (intent == null) return;

            FirebaseCloudMessagingImplementation.OnNewIntent(intent);

            // ✅ لو التطبيق اتفتح من نوتيفيكيشن المكالمة الواردة (زرار قبول أو جسم
            // النوتيفيكيشن نفسه)، خزّن بيانات المكالمة عشان الـ App.xaml.cs ينقل
            // المستخدم لصفحة المكالمة أول ما الـ Shell يخلص يتظبط.
            if (intent.GetStringExtra("tawseela_call_action") == "accept")
            {
                var orderId = intent.GetIntExtra("tawseela_order_id", 0);
                var callerName = intent.GetStringExtra("tawseela_caller_name") ?? "";
                if (orderId != 0)
                {
                    DeliveryApp.Customer.Services.PendingCallNavigation.OrderId = orderId;
                    DeliveryApp.Customer.Services.PendingCallNavigation.CallerName = callerName;
                }
            }
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

                // ✅ Tell Plugin.Firebase which channel to use
                FirebaseCloudMessagingImplementation.ChannelId = ChannelId;
                Android.Util.Log.Debug("FCM", "Notification channel created!");
            }
        }

        private void SetupLocalNotificationAction()
        {
            // ✅ This is the OFFICIAL Plugin.Firebase way to show local notifications.
            // Plugin.Firebase calls this action automatically when a message arrives —
            // both foreground AND background — so we never need NotificationReceived manually.
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

                    var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "");
                    if (intent != null)
                    {
                        intent.PutExtra(FirebaseCloudMessagingImplementation.IntentKeyFCMNotification,
                            notification.ToBundle());
                        intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
                    }

                    var pendingIntent = intent != null
                        ? PendingIntent.GetActivity(context, 0, intent,
                            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)
                        : null;

                    var builder = new NotificationCompat.Builder(context, ChannelId)
                        .SetContentTitle(notification.Title)
                        .SetContentText(notification.Body)
                        .SetPriority(NotificationCompat.PriorityHigh)
                        .SetDefaults(NotificationCompat.DefaultAll)
                        .SetAutoCancel(true)
                        .SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Body));

                    NotificationHelper.ApplyBranding(builder, context);

                    if (pendingIntent != null)
                        builder.SetContentIntent(pendingIntent);

                    var notificationManager = NotificationManagerCompat.From(context);
                    var notifId = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % int.MaxValue);
                    notificationManager.Notify(notifId, builder.Build());

                    Android.Util.Log.Debug("FCM", $"Local notification shown: {notification.Title}");
                }
                catch (Exception ex)
                {
                    Android.Util.Log.Error("FCM", $"ShowLocalNotificationAction error: {ex.Message}");
                }
            };
        }

        private void RequestNotificationPermissionIfNeeded()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                const string permission = "android.permission.POST_NOTIFICATIONS";
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                    ActivityCompat.RequestPermissions(this, new[] { permission }, NotificationPermissionRequestCode);
            }
        }
    }
}