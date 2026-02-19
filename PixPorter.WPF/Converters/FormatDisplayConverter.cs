using System.Globalization;
using System.Windows.Data;

namespace PixPorter.WPF.Converters;

public class FormatDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string s ? s.TrimStart('.').ToUpperInvariant() : "DEFAULT";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}