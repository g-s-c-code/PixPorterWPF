using System.Globalization;
using System.Windows.Data;

namespace PixPorter.WPF.Converters;

public class NullToDefaultFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? "Default" : ((string)value).TrimStart('.').ToUpperInvariant();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}