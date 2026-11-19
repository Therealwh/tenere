using System.Globalization;
using System.Windows.Data;

namespace Tenere.UI.Converters;

public sealed class BoolToOpacity : IValueConverter
{
    public double TrueValue { get; set; } = 1.0;
    public double FalseValue { get; set; } = 0.25;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = value is bool flag && flag;
        if (parameter is string text && text.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            result = !result;
        return result ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}