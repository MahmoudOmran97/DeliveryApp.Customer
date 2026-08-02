namespace DeliveryApp.Customer.Services;

// بيشيل بيانات النوتيفيكيشن العادي (مش مكالمة) لحد ما الـ Shell/App يخلصوا يتظبطوا
// وقت الفتح من نوتيفيكيشن (cold start أو warm start من الخلفية) عشان نقدر نوجّه
// المستخدم تلقائيًا لمكان محدد جوه التطبيق بدل ما يفتح على الهوم بس.
public static class PendingNotificationNavigation
{
    public static int? OrderId;
    public static string? Type;

    public static (int? orderId, string type)? TakePending()
    {
        if (OrderId is null && string.IsNullOrEmpty(Type)) return null;
        var result = (OrderId, Type ?? "General");
        OrderId = null;
        Type = null;
        return result;
    }
}
