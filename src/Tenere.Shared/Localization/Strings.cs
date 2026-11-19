using System.Globalization;
using System.Resources;

namespace Tenere.Shared.Localization;

public static class Strings
{
    private static readonly ResourceManager Manager =
        new("Tenere.Shared.Resources.Strings", typeof(Strings).Assembly);

    public static CultureInfo Culture
    {
        get => CultureInfo.CurrentUICulture;
        set => CultureInfo.CurrentUICulture = value;
    }

    public static string AppName => Get(nameof(AppName));
    public static string Tagline => Get(nameof(Tagline));
    public static string SearchPlaceholder => Get(nameof(SearchPlaceholder));
    public static string History => Get(nameof(History));
    public static string Collections => Get(nameof(Collections));
    public static string Settings => Get(nameof(Settings));
    public static string EmptyTitle => Get(nameof(EmptyTitle));
    public static string EmptyHint => Get(nameof(EmptyHint));
    public static string Paste => Get(nameof(Paste));
    public static string Pin => Get(nameof(Pin));
    public static string Delete => Get(nameof(Delete));
    public static string Privacy => Get(nameof(Privacy));
    public static string Ocr => Get(nameof(Ocr));
    public static string Language => Get(nameof(Language));
    public static string Theme => Get(nameof(Theme));
    public static string ClearAll => Get(nameof(ClearAll));
    public static string All => Get(nameof(All));
    public static string Images => Get(nameof(Images));
    public static string Links => Get(nameof(Links));
    public static string Code => Get(nameof(Code));
    public static string Text => Get(nameof(Text));

    private static string Get(string key) => Manager.GetString(key, Culture) ?? key;
}