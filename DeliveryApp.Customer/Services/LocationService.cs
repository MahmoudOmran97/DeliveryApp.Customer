// ═══════════════════════════════════════════════════════════════
// Services / LocationService.cs
// يحفظ ويسترجع موقع العميل المختار مع التحقق من الـ zone (10km)
// ═══════════════════════════════════════════════════════════════
namespace DeliveryApp.Customer.Services;

public class LocationService
{
    // ── مركز الـ zone (القاهرة الكبرى كمثال — غيّرها لمركز مدينتك) ──
    // يمكن تغييرها لاحقاً من الـ backend أو الـ settings
    public const double ZoneCenterLat = 30.0444;  // القاهرة
    public const double ZoneCenterLng = 31.2357;
    public const double ZoneRadiusKm  = 10.0;

    private const string K_Lat     = "user_lat";
    private const string K_Lng     = "user_lng";
    private const string K_Address = "user_address";
    private const string K_HasLoc  = "user_has_location";

    // ── Event يُطلق عند تغيير الموقع ──
    public event Action? LocationChanged;

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
