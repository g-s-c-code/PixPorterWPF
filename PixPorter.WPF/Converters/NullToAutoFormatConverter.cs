using System.Globalization;
using System.Windows.Data;

namespace PixPorter.WPF.Converters;

public class NullToAutoFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is null ? "Auto" : ((string)value).TrimStart('.').ToUpperInvariant();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}