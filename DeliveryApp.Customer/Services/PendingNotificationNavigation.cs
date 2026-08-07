namespace DeliveryApp.Customer.Services;

// بيشيل بيانات النوتيفيكيشن العادي (مش مكالمة) لحد ما الـ Shell/App يخلصوا يتظبطوا
// وقت الفتح من نوتيفيكيشن (cold start أو warm start من الخلفية) عشان نقدر نوجّه
// المستخدم تلقائيًا لمكان محدد جوه التطبيق بدل ما يفتح على الهوم بس.
public static class PendingNotificationNavigation
{
    public static int? OrderId;
    public static string? Type;
    public static string? ActionUrl;

    public static (int? orderId, string type, string? actionUrl)? TakePending()
    {
        if (OrderId is null && string.IsNullOrEmpty(Type) && string.IsNullOrEmpty(ActionUrl)) return null;
        var result = (OrderId, Type ?? "General", ActionUrl);
        OrderId = null;
        Type = null;
        ActionUrl = null;
        return result;
    }
}
