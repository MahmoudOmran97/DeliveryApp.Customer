using System.Globalization;

namespace DeliveryApp.Customer.Converters;

public class AmountToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int amount = 0;
        if (value is int i) amount = i;
        else if (value is decimal d) amount = (int)d;

        bool isEarned = amount > 0;
        bool isLight = parameter is string s && s == "Light";

        if (isEarned)
        {
            return isLight ? Color.FromArgb("#E8F5E9") : Color.FromArgb("#4CAF50");
        }
        else
        {
            return isLight ? Color.FromArgb("#FFEBEE") : Color.FromArgb("#F44336");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
