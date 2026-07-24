using System.Globalization;

namespace DeliveryApp.Customer.Converters;

// true → false, false → true
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        if (v is bool b) return !b;
        if (v is int i) return i <= 0;
        return true;
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => v is bool b && !b;
}

// int > 0 → true
public class IntToBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        bool result = false;
        if (v is int i) result = i > 0;
        else if (v is bool b) result = b;

        if (p is string s && s == "invert")
            return !result;

        return result;
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// bool true → Green bg, false → Red bg
public class IsOpenToColorConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is true ? Color.FromArgb("#E8F5E9") : Color.FromArgb("#FFEBEE");

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// unread notification → light orange bg
public class IsReadToColorConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is true ? Colors.White : Color.FromArgb("#FFF3EF");

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// null or empty string → false, any text → true
// Pass ConverterParameter="invert" to flip
public class NullOrEmptyToBoolConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        bool hasValue = v switch
        {
            string s => !string.IsNullOrWhiteSpace(s),
            null => false,
            _ => true
        };
        bool invert = p is string ps && ps == "invert";
        return invert ? !hasValue : hasValue;
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// bool true → first color, false → second color (format: "#color1|#color2")
// Fallback: true → Primary, false → gray
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        // Special case for PointsPage tabs
        if (v is string selectedTab && p is string targetTab)
        {
            bool isText = targetTab.EndsWith("_Text");
            string actualTarget = isText ? targetTab.Replace("_Text", "") : targetTab;
            bool isActive = selectedTab == actualTarget;

            if (isText)
                return isActive ? Colors.White : Color.FromArgb("#757575");
            else
                return isActive ? Color.FromArgb("#FF5722") : Colors.Transparent;
        }

        bool isTrue = v switch
        {
            bool b => b,
            int i => i > 0,
            _ => false
        };

        if (p is string param && param.Contains('|'))
        {
            var parts = param.Split('|');
            try { return Color.FromArgb(isTrue ? parts[0] : parts[1]); }
            catch { }
        }
        return isTrue ? Color.FromArgb("#FF5722") : Color.FromArgb("#E0E0E0");
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// بيقارن قيمة التقييم الحالية (int) برقم النجمة (ConverterParameter) عشان
// يحدد لو النجمة دي المفروض تبقى ذهبي (متعبّية) ولا رمادي (فاضية)
public class RatingStarColorConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        int rating = v switch
        {
            int i => i,
            double d => (int)d,
            _ => 0
        };
        int starIndex = p switch
        {
            string s when int.TryParse(s, out var idx) => idx,
            int i => i,
            _ => 0
        };
        return rating >= starIndex ? Color.FromArgb("#FFC107") : Color.FromArgb("#E0E0E0");
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// ⚠️ ملحوظة مهمة: إيموجي النجمة "⭐" ليه لون مدمج جوه الفونت نفسه ومش
// بيستجيب لـ TextColor خالص على أندرويد/iOS. عشان كده لازم نستخدم رمزين
// مختلفين فعليًا: ★ (ممتلئ) و ☆ (فاضي) بدل ما نعتمد على تلوين نفس الرمز.
public class RatingStarGlyphConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        int rating = v switch
        {
            int i => i,
            double d => (int)d,
            _ => 0
        };
        int starIndex = p switch
        {
            string s when int.TryParse(s, out var idx) => idx,
            int i => i,
            _ => 0
        };
        return rating >= starIndex ? "★" : "☆";
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}
public class FlowDirectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s && s.Equals("RightToLeft", StringComparison.OrdinalIgnoreCase))
            return FlowDirection.RightToLeft;

        return FlowDirection.LeftToRight;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}