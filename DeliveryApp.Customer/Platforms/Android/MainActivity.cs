using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Graphics;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using DeliveryApp.Customer.Platforms.Android;
using Microsoft.Maui.Storage;
using Plugin.Firebase.CloudMessaging;
using Plugin.Firebase.CloudMessaging.Platforms.Android.Extensions;
using Color = Android.Graphics.Color;

namespace DeliveryApp.Customer
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTask,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode
            | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationPermissionRequestCode = 1001;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // نلغي إطار النظام حول المحتوى: الخلفية ترسم حتى أعلى الشاشة خلف شريط
            // الساعة والبطارية، وتُستخدم مواضع ثابتة لعناصر الهيدر داخل الصفحة.
            if (Window != null)
            {
                // هاتان الدالتان مدعومتان في نسخة Android bindings بالمشروع.
                // لا نعدّل SystemUiVisibility ولا contrast enforcement لأنهما غير متوافقين هنا.
                WindowCompat.SetDecorFitsSystemWindows(Window, false);
                Window.SetStatusBarColor(Color.Transparent);
            }

            HandleIntent(Intent);
            RequestNotificationPermissionIfNeeded();
        }


        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            HandleIntent(intent);

            // ✅ لو التطبيق مفتوح قدامك (foreground) والإشعار اتضغط عليه مش هيحصل OnResume،
            // فبنوجّه من هنا مباشرة (والدالة بتتجاهل الطلب لو مفيش إشعار معلّق).
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                (Microsoft.Maui.Controls.Application.Current as DeliveryApp.Customer.App)?.TryNavigatePendingNotification());
        }

        private static void HandleIntent(Intent? intent)
        {
            if (intent == null) return;

            FirebaseCloudMessagingImplementation.OnNewIntent(intent);

            // ✅ لو التطبيق اتفتح من نوتيفيكيشن المكالمة الواردة:
            // - accept = زرار قبول → افتح المكالمة مع قبول تلقائي
            // - incoming = full-screen / جسم النوتيفيكيشن → افتح شاشة الرنين بس
            var callAction = intent.GetStringExtra("Taly_call_action");
            if (callAction is "accept" or "incoming")
            {
                var orderId = intent.GetIntExtra("Taly_order_id", 0);
                var callerName = intent.GetStringExtra("Taly_caller_name") ?? "";
                if (orderId != 0)
                {
                    DeliveryApp.Customer.Services.PendingCallNavigation.OrderId = orderId;
                    DeliveryApp.Customer.Services.PendingCallNavigation.CallerName = callerName;
                    DeliveryApp.Customer.Services.PendingCallNavigation.AutoAccept = callAction == "accept";
                }
                return;
            }

            // ✅ NAV FIX — نوتيفيكيشن عادي (مش مكالمة) اتضغط عليها — نسجل type/orderId/actionUrl
            // عشان App.xaml.cs يوجّه المستخدم للمكان الصح بمجرد ما التطبيق يفتح.
            // (وكمان نقرا مفاتيح FCM الخام type/orderId/actionUrl: لو النظام هو اللي عرض الإشعار
            // مباشرة — زي رسالة فيها notification block — بيحطها كـ extras على الـ intent بالاسم ده.)
            var notifType = intent.GetStringExtra("Taly_notif_type") ?? intent.GetStringExtra("type");
            var notifOrderIdStr = intent.GetStringExtra("Taly_notif_order_id") ?? intent.GetStringExtra("orderId");
            var notifActionUrl = intent.GetStringExtra("Taly_notif_action_url") ?? intent.GetStringExtra("actionUrl");
            if (!string.IsNullOrEmpty(notifType) || !string.IsNullOrEmpty(notifOrderIdStr) || !string.IsNullOrEmpty(notifActionUrl))
            {
                DeliveryApp.Customer.Services.PendingNotificationNavigation.Type = notifType;
                if (int.TryParse(notifOrderIdStr, out var notifOrderId) && notifOrderId != 0)
                    DeliveryApp.Customer.Services.PendingNotificationNavigation.OrderId = notifOrderId;
                if (!string.IsNullOrEmpty(notifActionUrl))
                    DeliveryApp.Customer.Services.PendingNotificationNavigation.ActionUrl = notifActionUrl;
            }
        }

        private void RequestNotificationPermissionIfNeeded()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                const string permission = "android.permission.POST_NOTIFICATIONS";
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                    ActivityCompat.RequestPermissions(this, new[] { permission }, NotificationPermissionRequestCode);
            }

            // Android 14+: إذن ظهور المكالمة فوق الشاشة (Full-Screen Intent) زي واتساب
            RequestFullScreenIntentPermissionIfNeeded();
        }

        // ✅ FIX: كانت الشاشة دي بتتفتح في كل مرة التطبيق بيفتح طول ما اليوزر
        // لسه ما وافقش على إذن الـ Full-Screen Intent — مفيش أي تسجيل إنها
        // اتعرضت قبل كده. دلوقتي بنسجلها في Preferences أول مرة تتعرض فيها
        // (سواء اليوزر وافق أو رفض)، فتتعرض مرة واحدة بس مدى عمر التطبيق
        // (زي وقت التسطيب)، ومش هتظهر تاني كل ما يفتح التطبيق.
        private const string FsiPromptShownKey = "fsi_permission_prompt_shown";

        private void RequestFullScreenIntentPermissionIfNeeded()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.UpsideDownCake) return;
            if (Preferences.Default.Get(FsiPromptShownKey, false)) return;

            try
            {
                var manager = GetSystemService(NotificationService) as NotificationManager;
                if (manager == null || manager.CanUseFullScreenIntent())
                {
                    Preferences.Default.Set(FsiPromptShownKey, true);
                    return;
                }

                // نسجل إن الشاشة اتعرضت قبل ما نفتحها فعليًا، عشان تتعرض مرة
                // واحدة بس حتى لو اليوزر رفض أو رجع من غير ما يوافق.
                Preferences.Default.Set(FsiPromptShownKey, true);

                var intent = new Intent(Android.Provider.Settings.ActionManageAppUseFullScreenIntent);
                intent.SetData(Android.Net.Uri.Parse($"package:{PackageName}"));
                StartActivity(intent);
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn("IncomingCall", $"FSI permission request failed: {ex.Message}");
            }
        }
    }
}