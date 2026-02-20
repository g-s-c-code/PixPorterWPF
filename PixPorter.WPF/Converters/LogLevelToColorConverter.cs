using PixPorter.WPF.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PixPorter.WPF.Converters;

public class LogLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LogLevel level)
            return new SolidColorBrush(Colors.Transparent);

        return level switch
        {
            LogLevel.Success => GetThemeBrush("SuccessBrush"),
            LogLevel.Error => GetThemeBrush("DestructiveBrush"),
            _ => GetThemeBrush("TextSecondaryBrush")
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