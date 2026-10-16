// ═══════════════════════════════════════════════════════════════
// Helpers / DeviceLocationHelper.cs
// ═══════════════════════════════════════════════════════════════
// ✅ FIX: تحديد الموقع التلقائي كان أحيانًا بيفشل بدون أي رسالة (GPS مقفول، الصلاحية
// مرفوضة، أو انتهت مهلة الـ 8 ثواني بدقة Medium فبنقع على القاهرة بدون ما العميل يعرف).
// الكلاس ده بيجمّع المنطق في مكان واحد:
//   1) يطلب الصلاحية لو لسه.
//   2) يجرب دقة عالية بمهلة 15 ثانية.
//   3) لو فشل/انتهت المهلة يجرب آخر موقع معروف للجهاز.
//   4) يرجّع سبب الفشل بشكل واضح عشان الصفحة تعرض رسالة مفهومة للعميل.
// ═══════════════════════════════════════════════════════════════
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer.Helpers;

public enum LocationFailure
{
    None,
    PermissionDenied,
    GpsDisabled,
    NotSupported,
    Unavailable
}

public static class DeviceLocationHelper
{
    /// <summary>لازم تتنادى من الـ UI thread (عشان طلب الصلاحية).</summary>
    public static async Task<(Location? Location, LocationFailure Failure)> GetCurrentAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
                return (null, LocationFailure.PermissionDenied);

            Location? loc = null;
            try
            {
                loc = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15)));
            }
            catch (FeatureNotEnabledException) { return (null, LocationFailure.GpsDisabled); }
            catch (FeatureNotSupportedException) { return (null, LocationFailure.NotSupported); }
            catch (PermissionException) { return (null, LocationFailure.PermissionDenied); }
            catch { /* timeout أو خطأ مؤقت — هنجرب آخر موقع معروف تحت */ }

            if (loc != null) return (loc, LocationFailure.None);

            try
            {
                var last = await Geolocation.Default.GetLastKnownLocationAsync();
                if (last != null) return (last, LocationFailure.None);
            }
            catch { }

            return (null, LocationFailure.Unavailable);
        }
        catch (PermissionException) { return (null, LocationFailure.PermissionDenied); }
        catch { return (null, LocationFailure.Unavailable); }
    }

    /// <summary>بيعرض رسالة مناسبة لسبب فشل تحديد الموقع (وزرار فتح الإعدادات لو الصلاحية مرفوضة).</summary>
    public static async Task ShowFailureAsync(Page page, LocationFailure failure)
    {
        switch (failure)
        {
            case LocationFailure.PermissionDenied:
                var openSettings = await page.DisplayAlert(
                    LocalizationService.Get("Notice"),
                    LocalizationService.Get("LocationPermissionDenied"),
                    LocalizationService.Get("OpenSettings"),
                    LocalizationService.Get("Cancel"));
                if (openSettings)
                {
                    try { AppInfo.Current.ShowSettingsUI(); } catch { }
                }
                break;

            case LocationFailure.GpsDisabled:
                await page.DisplayAlert(
                    LocalizationService.Get("Notice"),
                    LocalizationService.Get("LocationServiceDisabled"),
                    LocalizationService.Get("Ok"));
                break;

            case LocationFailure.NotSupported:
            case LocationFailure.Unavailable:
                await page.DisplayAlert(
                    LocalizationService.Get("Notice"),
                    LocalizationService.Get("LocationUnavailable"),
                    LocalizationService.Get("Ok"));
                break;
        }
    }
}
