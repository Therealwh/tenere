using System.Globalization;
using System.Windows.Data;

namespace Tenere.UI.Converters;

public sealed class DateToRelative : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        DateTimeOffset moment;
        switch (value)
        {
            case DateTimeOffset offset:
                moment = offset;
                break;
            case DateTime date:
                moment = new DateTimeOffset(date);
                break;
            default:
                return string.Empty;
        }

        var delta = DateTimeOffset.UtcNow - moment;
        if (delta < TimeSpan.Zero) delta = TimeSpan.Zero;

        if (delta < TimeSpan.FromMinutes(1)) return "now";
        if (delta < TimeSpan.FromHours(1)) return $"{(int)delta.TotalMinutes}m ago";
        if (delta < TimeSpan.FromDays(1)) return $"{(int)delta.TotalHours}h ago";
        if (delta < TimeSpan.FromDays(7)) return $"{(int)delta.TotalDays}d ago";
        return moment.LocalDateTime.ToString("MMM d", culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}