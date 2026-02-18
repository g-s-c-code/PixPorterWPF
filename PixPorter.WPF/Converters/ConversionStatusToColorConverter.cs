using PixPorter.WPF.ViewModels;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PixPorter.WPF.Converters;

public class ConversionStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConversionStatus status)
        {
            return status switch
            {
                ConversionStatus.Done => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                ConversionStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 68, 68)),
                ConversionStatus.Converting => new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                _ => new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}