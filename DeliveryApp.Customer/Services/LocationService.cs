// ═══════════════════════════════════════════════════════════════
// Services / LocationService.cs
// يحفظ ويسترجع موقع العميل المختار مع التحقق من الـ zone
// الزون (أقصى مسافة توصيل) بقى قابل للتعديل من لوحة الأدمن (DeliverySettings.MaxDeliveryZoneKm)
// بدل ما كان رقم ثابت 10 كم في الكود. بنكاشه محليًا (Preferences) عشان يشتغل حتى
// لو النت وقع، وبنعمله Refresh من السيرفر أول ما نقدر (HomeViewModel/CategoryViewModel/CheckoutViewModel).
// ═══════════════════════════════════════════════════════════════
namespace DeliveryApp.Customer.Services;

public class LocationService
{
    // ── مركز الـ zone (القاهرة الكبرى كمثال — غيّرها لمركز مدينتك) ──
    public const double ZoneCenterLat = 30.0444;  // القاهرة
    public const double ZoneCenterLng = 31.2357;

    // ── القيمة الافتراضية (Fallback) لو لسه معملناش Refresh من السيرفر ──
    public const double DefaultZoneRadiusKm = 10.0;

    private const string K_Lat          = "user_lat";
    private const string K_Lng          = "user_lng";
    private const string K_Address      = "user_address";
    private const string K_HasLoc       = "user_has_location";
    private const string K_ZoneRadius   = "delivery_zone_radius_km";
    private const string K_ZoneReason   = "delivery_zone_reason";

    // ── Event يُطلق عند تغيير الموقع ──
    public event Action? LocationChanged;

    /// <summary>أقصى مسافة توصيل حالية (الزون) — بتترجع من الكاش، ومحدثة من السيرفر لو حصل Refresh</summary>
    public double ZoneRadiusKm => Preferences.Get(K_ZoneRadius, DefaultZoneRadiusKm);

    /// <summary>
    /// سبب تقليل الزون لو الأدمن حدد واحد (مثلاً تقليل مؤقت بالليل)، وإلا null فبتتعرض رسالة عامة.
    /// </summary>
    public string? ZoneReducedReason
    {
        get
        {
            var v = Preferences.Get(K_ZoneReason, string.Empty);
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }
    }

    /// <summary>
    /// بيجيب أحدث إعدادات الزون من السيرفر ويكاشها محليًا. يتنادى عادة أول ما الصفحة تفتح.
    /// لو فشل (مفيش نت مثلاً) بيسيب القيمة القديمة في الكاش زي ما هي.
    /// </summary>
    public async Task RefreshZoneAsync(ApiService api)
    {
        try
        {
            var settings = await api.GetDeliverySettingsAsync();
            if (settings != null && settings.MaxDeliveryZoneKm > 0)
            {
                Preferences.Set(K_ZoneRadius, settings.MaxDeliveryZoneKm);
                Preferences.Set(K_ZoneReason, settings.ZoneReducedReason ?? string.Empty);
            }
        }
        catch
        {
            // تجاهل: هنكمل بالقيمة المكاشة أو الافتراضية
        }
    }

    public bool HasLocation => Preferences.Get(K_HasLoc, false);

    public double Latitude  => Preferences.Get(K_Lat, ZoneCenterLat);
    public double Longitude => Preferences.Get(K_Lng, ZoneCenterLng);
    public string AddressLabel => Preferences.Get(K_Address, string.Empty);

    // ── حفظ الموقع ──
    public void SaveLocation(double lat, double lng, string? label = null)
    {
        Preferences.Set(K_Lat, lat);
        Preferences.Set(K_Lng, lng);
        Preferences.Set(K_Address, label ?? $"{lat:F4}, {lng:F4}");
        Preferences.Set(K_HasLoc, true);
        LocationChanged?.Invoke();
    }

    // ── مسح الموقع ──
    public void ClearLocation()
    {
        Preferences.Remove(K_Lat);
        Preferences.Remove(K_Lng);
        Preferences.Remove(K_Address);
        Preferences.Set(K_HasLoc, false);
        LocationChanged?.Invoke();
    }

    // ── التحقق إذا كان الموقع داخل الـ zone (10km) ──
    public bool IsWithinZone(double lat, double lng)
        => DistanceKm(lat, lng, ZoneCenterLat, ZoneCenterLng) <= ZoneRadiusKm;

    // ── حساب المسافة بين نقطتين بالكيلومتر (Haversine) ──
    public static double DistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0; // نصف قطر الأرض
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    static double ToRad(double deg) => deg * Math.PI / 180.0;
}
