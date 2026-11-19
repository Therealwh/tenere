using System.Globalization;
using System.Windows.Data;
using Tenere.Core.Models;

namespace Tenere.UI.Converters;

public sealed class ContentTypeToIcon : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ClipContentType type)
            return Glyphs[ClipContentType.Text];

        return Glyphs.TryGetValue(type, out var glyph) ? glyph : Glyphs[ClipContentType.Text];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;

    private static readonly IReadOnlyDictionary<ClipContentType, string> Glyphs =
        new Dictionary<ClipContentType, string>
        {
            [ClipContentType.Text] = "\uE8FD",
            [ClipContentType.Link] = "\uE71B",
            [ClipContentType.Code] = "\uE943",
            [ClipContentType.Image] = "\uEB9F",
            [ClipContentType.Color] = "\uE790",
            [ClipContentType.Table] = "\uE80B",
            [ClipContentType.File] = "\uE8A5"
        };
}