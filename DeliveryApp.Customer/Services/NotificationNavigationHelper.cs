namespace DeliveryApp.Customer.Services;

// ─────────────────────────────────────────────────────────────────────────
// بيفك تشفير ActionUrl (نفس نظام البانرات بالظبط: "order/5", "restaurant/12",
// "chat/5", "category/food"، أو رابط خارجي https://...) ويوجّه المستخدم
// للمكان الصح جوه التطبيق. مستخدم من App.xaml.cs (فتح من نوتيفيكيشن) ومن
// NotificationsViewModel (ضغط على عنصر في شاشة الإشعارات نفسها).
// ─────────────────────────────────────────────────────────────────────────
public static class NotificationNavigationHelper
{
    public static async Task NavigateAsync(string? actionUrl, int? fallbackOrderId = null)
    {
        var url = (actionUrl ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(url))
        {
            // مفيش ActionUrl صريح — رجوع للسلوك القديم: orderId لو موجود، وإلا شاشة الإشعارات
            if (fallbackOrderId.HasValue)
                await Shell.Current.GoToAsync($"OrderDetailPage?orderId={fallbackOrderId}");
            else
                await Shell.Current.GoToAsync("NotificationsPage");
            return;
        }

        try
        {
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                await Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
                return;
            }

            var parts = url.Split('/', 2);
            var type = parts[0].ToLowerInvariant();
            var value = parts.Length > 1 ? parts[1] : string.Empty;

            switch (type)
            {
                case "order" when int.TryParse(value, out var orderId) && orderId > 0:
                    await Shell.Current.GoToAsync($"OrderDetailPage?orderId={orderId}");
                    break;

                case "restaurant" or "store" when int.TryParse(value, out var restaurantId) && restaurantId > 0:
                    await Shell.Current.GoToAsync($"RestaurantPage?id={restaurantId}");
                    break;

                case "chat" when int.TryParse(value, out var chatOrderId) && chatOrderId > 0:
                    await Shell.Current.GoToAsync($"DriverChatPage?orderId={chatOrderId}");
                    break;

                case "category" when !string.IsNullOrWhiteSpace(value):
                    await Shell.Current.GoToAsync($"CategoryPage?category={Uri.EscapeDataString(value)}");
                    break;

                default:
                    // صيغة غير معروفة — بدل ما نفضل واقفين، افتح شاشة الإشعارات
                    await Shell.Current.GoToAsync("NotificationsPage");
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Notif] ActionUrl navigate failed: {ex.Message}");
        }
    }
}
