using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tenere.UI.Converters;

public sealed class NullToVisibility : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var hasValue = value switch
        {
            null => false,
            string text => !string.IsNullOrWhiteSpace(text),
            _ => true
        };
        if (parameter is string parameterText && parameterText.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}