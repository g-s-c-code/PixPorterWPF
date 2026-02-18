using PixPorter.WPF.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PixPorter.WPF.Converters;

public class ConversionStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ConversionStatus status)
            return new SolidColorBrush(Colors.Transparent);

        return status switch
        {
            ConversionStatus.Done => GetThemeBrush("SuccessBrush"),
            ConversionStatus.Failed => GetThemeBrush("DestructiveBrush"),
            ConversionStatus.Converting => new SolidColorBrush(Color.FromRgb(160, 160, 160)),
            _ => new SolidColorBrush(Color.FromRgb(100, 100, 100))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

    private static SolidColorBrush GetThemeBrush(string key)
    {
        if (Application.Current.Resources[key] is SolidColorBrush brush)
            return brush;
        return new SolidColorBrush(Colors.Transparent);
    }
}