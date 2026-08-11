using System.ComponentModel;

namespace DeliveryApp.Customer.Services;

// ✅ FIX (تصميم الغلاف/الأيقونات كانت بتتخبط مع الساعة/الشحن/الشبكة): قبل كده
// كانت أماكن زرار الرجوع/البحث/السلة فوق الغلاف Margin ثابت (45 / 48 / 101px)
// متخمّن على قد ارتفاع شريط الحالة (status bar) وقتها. بعد ما التطبيق بقى
// مستهدف Android 15 (edge-to-edge إجباري)، المحتوى بقى بيترسم فعليًا تحت شريط
// الحالة، فالأرقام الثابتة دي بقت غلط على شاشات كتير (خصوصًا اللي عندها notch/
// كاميرا بانش-هول)، وده اللي كان بيبين إن الأيقونات "ملزّقة" في الساعة/الشحن/
// الشبكة أو بعيدة عنها أوي حسب الجهاز.
//
// الحل: بنقيس ارتفاع شريط الحالة الحقيقي وقت الرن (مش رقم ثابت) من كل منصة،
// ونحطه هنا في مكان واحد، وبعدين RestaurantPage.xaml.cs بيستخدمه عشان يحط
// كل الأيقونات في نفس المسافة الصح على أي جهاز.
public static class SafeAreaService
{
    // قيمة افتراضية معقولة (لو المنصة لسه ما قاستش الإنسيت الحقيقي وقت أول رسم)
    // عشان الشاشة متبانش ملزّقة قبل ما القياس الحقيقي يوصل.
    const double FallbackTopInset = 28;

    static double _topInset = FallbackTopInset;

    public static double TopInset
    {
        get => _topInset;
        set
        {
            // بعض المنصات بتبعت 0 لحظة الإطلاق الأولى قبل ما الـ layout يخلص؛
            // متجاهلينها عشان مانرجعش لمسافة صفر غلط.
            if (value <= 0) return;
            if (Math.Abs(_topInset - value) < 0.5) return;
            _topInset = value;
            TopInsetChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    // الصفحات المفتوحة أصلاً (زي صفحة المحل) بتسمع للحدث ده عشان تحدّث
    // الهوامش فورًا لو القياس الحقيقي وصل بعد ما الصفحة اتفتحت.
    public static event EventHandler? TopInsetChanged;
}
