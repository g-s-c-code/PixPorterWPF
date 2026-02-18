using System.Globalization;
using System.Windows.Data;

namespace PixPorter.WPF.Converters;

public class FormatDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s)
            return s.TrimStart('.').ToUpperInvariant();

        return "AUTO";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}