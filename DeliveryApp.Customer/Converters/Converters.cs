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
        if (v is int i) return i > 0;
        if (v is bool b) return b;
        return false;
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