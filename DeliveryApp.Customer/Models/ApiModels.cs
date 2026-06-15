// ═══════════════════════════════════════════════════════════════
// DeliveryApp.Customer / Models / ApiModels.cs
// ═══════════════════════════════════════════════════════════════
using CommunityToolkit.Mvvm.ComponentModel;

namespace DeliveryApp.Customer.Models;

// ─── Auth ────────────────────────────────────────────────────────────────────

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// ─── User ────────────────────────────────────────────────────────────────────

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Restaurant ──────────────────────────────────────────────────────────────

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? ImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Phone { get; set; }
    public double Rating { get; set; }
    public int TotalRatings { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int EstimatedTime { get; set; }
    public bool IsOpen { get; set; }
    public double? DistanceKm { get; set; }
    public string StoreType { get; set; } = "Restaurant";

    private const string _imgBase = "https://deliveryappapi.runasp.net";
    private static string? BuildUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
        return _imgBase + (url.StartsWith("/") ? url : "/" + url);
    }

    public string? FullImageUrl => BuildUrl(ImageUrl);
    public string? FullCoverImageUrl => BuildUrl(CoverImageUrl);
    public string RatingText => $"{Rating:F1} ★";
    public string DeliveryFeeText => DeliveryFee == 0 ? "Free Delivery" : $"{DeliveryFee:F0} EGP";
    public string EstimatedTimeText => $"{EstimatedTime} min";
    public string StatusText => IsOpen ? "Open" : "Closed";
    public Color StatusColor => IsOpen ? Colors.Green : Colors.Red;
    public Color StatusBg => IsOpen ? Color.FromArgb("#E8F5E9") : Color.FromArgb("#FFEBEE");
}

// ─── Menu ────────────────────────────────────────────────────────────────────

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<Product> Products { get; set; } = new();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public string? ImageUrl { get; set; }
    public int PreparationTime { get; set; }
    public int? Calories { get; set; }
    public bool IsAvailable { get; set; }

    private const string _pImgBase = "https://deliveryappapi.runasp.net";
    public string? FullImageUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ImageUrl)) return null;
            if (ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return ImageUrl;
            return _pImgBase + (ImageUrl.StartsWith("/") ? ImageUrl : "/" + ImageUrl);
        }
    }

    public decimal EffectivePrice => DiscountedPrice ?? Price;
    public bool HasDiscount => DiscountedPrice.HasValue && DiscountedPrice < Price;
    public string PriceText => $"{EffectivePrice:F0} EGP";
    public string OriginalPriceText => $"{Price:F0} EGP";
}

// ─── Cart ────────────────────────────────────────────────────────────────────

public partial class CartItem : ObservableObject
{
    public int RestaurantId { get; set; }
    public Product Product { get; set; } = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    [NotifyPropertyChangedFor(nameof(TotalPriceText))]
    private int _Quantity;

    public string? Notes { get; set; }
    public decimal TotalPrice => Product.EffectivePrice * _Quantity;
    public string TotalPriceText => $"{TotalPrice:F0} EGP";
}

// ─── Orders ──────────────────────────────────────────────────────────────────

public class Order
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAmount { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }
    public string? DeliveryNotes { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int? EstimatedDelivery { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? RestaurantName { get; set; }
    public string? RestaurantImage { get; set; }
    public int ItemCount { get; set; }

    public OrderDriverInfo? Driver { get; set; }
    public OrderRestaurantInfo? Restaurant { get; set; }
    public List<OrderItem> Items { get; set; } = new();

    public string TotalAmountText => $"{TotalAmount:F0} EGP";

    public string StatusText => Status switch
    {
        "Pending" => "Waiting for restaurant",
        "Accepted" => "Order accepted ✓",
        "Preparing" => "Preparing your food 🍳",
        "ReadyForPickup" => "Ready — waiting for driver",
        "OnTheWay" => "Driver on the way 🛵",
        "Delivered" => "Delivered ✓",
        "Cancelled" => "Cancelled",
        "Rejected" => "Rejected",
        _ => Status
    };

    public Color StatusColor => Status switch
    {
        "Delivered" => Color.FromArgb("#4CAF50"),
        "Cancelled" or "Rejected" => Color.FromArgb("#F44336"),
        "OnTheWay" or "ReadyForPickup" => Color.FromArgb("#FF5722"),
        _ => Color.FromArgb("#FF9800")
    };

    public bool IsActive => Status is "Pending" or "Accepted" or "Preparing" or "ReadyForPickup" or "OnTheWay";
    public bool CanCancel => Status is "Pending" or "Accepted";
    public bool CanRate => Status == "Delivered";
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Notes { get; set; }

    public string TotalPriceText => $"{TotalPrice ?? UnitPrice * Quantity:F0} EGP";
}

public class OrderDriverInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public double Rating { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
}

public class OrderRestaurantInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Phone { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

// ─── Notifications ───────────────────────────────────────────────────────────

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }

    public string TimeText
    {
        get
        {
            var d = DateTime.UtcNow - CreatedAt;
            if (d.TotalMinutes < 1) return "Just now";
            if (d.TotalHours < 1) return $"{(int)d.TotalMinutes} min ago";
            if (d.TotalDays < 1) return $"{(int)d.TotalHours} hr ago";
            return CreatedAt.ToString("MMM dd");
        }
    }

    public Color BackgroundColor => IsRead ? Colors.White : Color.FromArgb("#FFF3EF");
}

// ─── Paged Result ─────────────────────────────────────────────────────────────

public class PagedResult<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int? TotalPages { get; set; }
    public List<T> Data { get; set; } = new();
}

// ─── Support Chat (AI) ───────────────────────────────────────────────────────

public class ChatMessage
{
    public string Text { get; set; } = string.Empty;
    public bool IsFromAi { get; set; }
    public bool IsFromUser => !IsFromAi;
    public DateTime Time { get; set; } = DateTime.Now;
    public string TimeDisplay => Time.ToString("hh:mm tt");
}

// ─── Driver Chat ─────────────────────────────────────────────────────────────

public class DriverChatMessage
{
    // ✅ FIX #2 — السيرفر بيرجع field اسمه "message" مش "text"
    // بدون JsonPropertyName، الـ deserializer مش بيملّي Text وبيفضل فاضي
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public string Text { get; set; } = string.Empty;
    public bool IsFromMe { get; set; }
    public bool IsFromOther => !IsFromMe;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string TimeDisplay => Timestamp.ToString("hh:mm tt");
}

// ─── Chat Notification (in-app banner) ───────────────────────────────────────

public class ChatNotification
{
    public int OrderId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
}

// ─── Banner ───────────────────────────────────────────────────────────────────

public class Banner
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? SubTitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? ActionUrl { get; set; }
    public string? BackgroundColor { get; set; }
    public int SortOrder { get; set; }

    private const string _base = "https://deliveryappapi.runasp.net";
    public string? FullImageUrl
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ImageUrl)) return null;
            if (ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return ImageUrl;
            return _base + (ImageUrl.StartsWith("/") ? ImageUrl : "/" + ImageUrl);
        }
    }

    public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

    public Color BgColor
    {
        get
        {
            try { return Color.FromArgb(BackgroundColor ?? "#FF5722"); }
            catch { return Color.FromArgb("#FF5722"); }
        }
    }
}

// ─── Coupon ───────────────────────────────────────────────────────────────────

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "Fixed";
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? ExpiresInDays { get; set; }

    public string DiscountText => DiscountType == "Percentage"
        ? $"{DiscountValue:F0}%"
        : $"{DiscountValue:F0} جنيه";

    public string MinOrderText => MinOrderAmount.HasValue
        ? $"الحد الأدنى {MinOrderAmount:F0} جنيه"
        : "بدون حد أدنى";

    public string ExpiryText => ExpiresInDays.HasValue
        ? (ExpiresInDays <= 0 ? "منتهي" : $"ينتهي بعد {ExpiresInDays} يوم")
        : "بدون تاريخ انتهاء";

    public bool IsExpiringSoon => ExpiresInDays.HasValue && ExpiresInDays <= 3 && ExpiresInDays > 0;

    public string Status { get; set; } = "Available"; // Available, Used, Expired

    public string StatusText => Status switch
    {
        "Used" => "تم استخدامه",
        "Expired" => "منتهي الصلاحية",
        _ => "متاح"
    };

    public string StatusColor => Status switch
    {
        "Used" => "#9E9E9E",
        "Expired" => "#F44336",
        _ => "#4CAF50"
    };

    public Color ExpiryColor => IsExpiringSoon ? Colors.Red : Color.FromArgb("#757575");
}

// ─── Deal ─────────────────────────────────────────────────────────────────────

public class Deal
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? RestaurantImage { get; set; }
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int? DiscountPercent { get; set; }
    public string? BadgeText { get; set; }
    public string? BadgeColor { get; set; }
    public int SortOrder { get; set; }

    public string PriceText => DiscountedPrice.HasValue ? $"{DiscountedPrice:F0} جنيه" : "";
    public string OriginalPriceText => OriginalPrice.HasValue ? $"{OriginalPrice:F0} جنيه" : "";
    public bool HasDiscount => DiscountedPrice.HasValue && OriginalPrice.HasValue && DiscountedPrice < OriginalPrice;

    public Color BadgeColorValue
    {
        get
        {
            try { return Color.FromArgb(BadgeColor ?? "#F44336"); }
            catch { return Colors.Red; }
        }
    }
}