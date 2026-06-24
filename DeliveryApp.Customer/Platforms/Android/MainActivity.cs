using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace DeliveryApp.Customer
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationPermissionRequestCode = 1001;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ✅ Notification Channel لازم يتعمل الأول
            CreateNotificationChannel();

            // ✅ بعدين اطلب الـ Permission
            RequestNotificationPermissionIfNeeded();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    "default",
                    "General Notifications",
                    NotificationImportance.High)
                {
                    Description = "Default notification channel",
                    LockscreenVisibility = NotificationVisibility.Public
                };

                channel.EnableLights(true);
                channel.EnableVibration(true);

                var manager = (NotificationManager)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);

                Android.Util.Log.Debug("FCM", "Notification channel created!");
            }
        }

        private void RequestNotificationPermissionIfNeeded()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                const string permission = "android.permission.POST_NOTIFICATIONS";
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, new[] { permission }, NotificationPermissionRequestCode);
                }
            }
        }
    }
}