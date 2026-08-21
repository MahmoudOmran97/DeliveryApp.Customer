using Android.App;
using Android.Content;
using AndroidX.Core.App;
using System.Net.Http;

namespace DeliveryApp.Customer.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = false, Label = "Taly Call Actions")]
[IntentFilter(new[] { IncomingCallNotificationHelper.ActionAccept, IncomingCallNotificationHelper.ActionReject })]
public class CallActionReceiver : BroadcastReceiver
{
    const string ProductionBaseUrl = "https://deliveryappapi.runasp.net/api";
    const string TokenPrefKey = "auth_token"; // لازم يتطابق مع AuthService.K_Token

    // 🔧 PERF FIX: instance واحد مشترك بدل new HttpClient() لكل رفض مكالمة.
    static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        var orderId = intent.GetIntExtra("orderId", 0);
        var notifId = intent.GetIntExtra("notificationId", 0);

        if (notifId != 0)
            NotificationManagerCompat.From(context).Cancel(notifId);

        if (intent.Action == IncomingCallNotificationHelper.ActionAccept)
        {
            var callerName = intent.GetStringExtra("callerName") ?? "";
            var launch = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? "");
            if (launch != null)
            {
                launch.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                launch.PutExtra("Taly_call_action", "accept");
                launch.PutExtra("Taly_order_id", orderId);
                launch.PutExtra("Taly_caller_name", callerName);
                context.StartActivity(launch);
            }
        }
        else if (intent.Action == IncomingCallNotificationHelper.ActionReject)
        {
            // مفيش داعي نفتح التطبيق عشان نرفض؛ نبعت الرفض مباشرة بـ REST call خفيف
            var pending = GoAsync();
            _ = Task.Run(async () =>
            {
                try { await RejectCallRestAsync(orderId); }
                catch (Exception ex) { global::Android.Util.Log.Error("CallActionReceiver", $"Reject failed: {ex.Message}"); }
                finally { pending.Finish(); }
            });
        }
    }

    static async Task RejectCallRestAsync(int orderId)
    {
        if (orderId == 0) return;

        // 🔒 SECURITY FIX: كان بيقرا من Preferences (plaintext) — بعد ما AuthService
        // بقى بيخزن في SecureStorage، لازم نقرا من نفس المكان وإلا زرار "رفض
        // المكالمة" من الإشعار هيبطل يشتغل لأي مستخدم يعمل login بعد التحديث ده.
        var token = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync(TokenPrefKey);
        if (string.IsNullOrEmpty(token)) return;

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{ProductionBaseUrl}/voicecall/reject/{orderId}");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _http.SendAsync(request);
    }
}
