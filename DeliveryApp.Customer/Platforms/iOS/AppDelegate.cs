using System.Linq;
using Foundation;
using UIKit;
using DeliveryApp.Customer.Services;

namespace DeliveryApp.Customer
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        // ✅ FIX: نفس فكرة MainActivity على أندرويد — بنقيس مساحة الـ Safe Area
        // الحقيقية فوق (شريط الحالة/الـ notch) على آيفون بدل رقم ثابت في الـ XAML،
        // عشان الأيقونات فوق الغلاف تقف في نفس المكان الصح على أي موديل آيفون.
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var result = base.FinishedLaunching(application, launchOptions);
            CaptureSafeAreaTopInset();
            return result;
        }

        static void CaptureSafeAreaTopInset()
        {
            // أول Layout بياخد لحظة عشان يخلص، فبنقيس بعد Tick بسيط ونعيد المحاولة
            // مرة كمان بعد نص ثانية للاطمئنان (مهم خصوصًا أول تشغيل للتطبيق).
            TryCapture();
            DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, 500_000_000), TryCapture);
        }

        static void TryCapture()
        {
            var window = UIApplication.SharedApplication.ConnectedScenes
                .OfType<UIWindowScene>()
                .SelectMany(s => s.Windows)
                .FirstOrDefault(w => w.IsKeyWindow)
                ?? UIApplication.SharedApplication.Windows.FirstOrDefault();

            var top = window?.SafeAreaInsets.Top ?? 0;
            if (top > 0)
                SafeAreaService.TopInset = top;
        }
    }
}
