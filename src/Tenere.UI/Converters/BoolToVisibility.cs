using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tenere.UI.Converters;

public sealed class BoolToVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var visible = value is bool flag && flag;
        if (parameter is string text && text.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            visible = !visible;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}