using System.Globalization;

namespace DeliveryApp.Customer.Converters;

// true → false, false → true

public class InvertedBoolConverter : IValueConverter

{

    public object Convert(object? v, Type t, object? p, CultureInfo c)

        => v is bool b && !b;

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)

        => v is bool b && !b;

}

// int > 0 → true

public class IntToBoolConverter : IValueConverter

{

    public object Convert(object? v, Type t, object? p, CultureInfo c)

        => v is int i && i > 0;

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

