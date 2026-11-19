using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Tenere.Shared.Settings;

namespace Tenere.UI;

public static class ThemeManager
{
    public static bool SystemUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not 0;
        }
        catch
        {
            return true;
        }
    }

    public static void Apply(ResourceDictionary resources, AppSettings settings, bool systemUsesLightTheme)
    {
        var dark = settings.Appearance.Theme switch
        {
            "dark" => true,
            "light" => false,
            _ => !systemUsesLightTheme
        };

        ApplyPalette(resources, dark);

        var hue = settings.Appearance.AccentHue;
        resources["AccentBrush"] = Brush(HslToRgb(hue, 0.72, 0.48));
        resources["AccentHoverBrush"] = Brush(HslToRgb(hue, 0.76, 0.58));
        resources["AccentPressedBrush"] = Brush(HslToRgb(hue, 0.80, 0.40));
        resources["AccentSoftBrush"] = Brush(HslToRgb(hue, 0.70, dark ? 0.30 : 0.91, dark ? 0.28 : 1.0));
    }

    private static void ApplyPalette(ResourceDictionary resources, bool dark)
    {
        if (dark)
        {
            resources["WindowBackgroundBrush"] = Brush(Color.FromRgb(0x16, 0x18, 0x1C));
            resources["SidebarBackgroundBrush"] = Brush(Color.FromRgb(0x1C, 0x1F, 0x24));
            resources["CardBackgroundBrush"] = Brush(Color.FromRgb(0x21, 0x24, 0x2B));
            resources["CardHoverBackgroundBrush"] = Brush(Color.FromRgb(0x27, 0x2B, 0x33));
            resources["InputBackgroundBrush"] = Brush(Color.FromRgb(0x21, 0x24, 0x2B));
            resources["ChipBackgroundBrush"] = Brush(Color.FromRgb(0x25, 0x28, 0x2F));
            resources["StatusBarBackgroundBrush"] = Brush(Color.FromRgb(0x1C, 0x1F, 0x24));
            resources["BorderBrush"] = Brush(Color.FromRgb(0x33, 0x37, 0x40));
            resources["TextPrimaryBrush"] = Brush(Color.FromRgb(0xF2, 0xF4, 0xF8));
            resources["TextSecondaryBrush"] = Brush(Color.FromRgb(0xA8, 0xAF, 0xBC));
            resources["TextTertiaryBrush"] = Brush(Color.FromRgb(0x70, 0x78, 0x86));
            resources["SubtleIconBrush"] = Brush(Color.FromRgb(0x8A, 0x92, 0xA0));
        }
        else
        {
            resources["WindowBackgroundBrush"] = Brush(Color.FromRgb(0xF6, 0xF7, 0xFA));
            resources["SidebarBackgroundBrush"] = Brush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            resources["CardBackgroundBrush"] = Brush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            resources["CardHoverBackgroundBrush"] = Brush(Color.FromRgb(0xF1, 0xF4, 0xF9));
            resources["InputBackgroundBrush"] = Brush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            resources["ChipBackgroundBrush"] = Brush(Color.FromRgb(0xEE, 0xF1, 0xF6));
            resources["StatusBarBackgroundBrush"] = Brush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            resources["BorderBrush"] = Brush(Color.FromRgb(0xE1, 0xE5, 0xEC));
            resources["TextPrimaryBrush"] = Brush(Color.FromRgb(0x1B, 0x20, 0x2A));
            resources["TextSecondaryBrush"] = Brush(Color.FromRgb(0x5C, 0x65, 0x75));
            resources["TextTertiaryBrush"] = Brush(Color.FromRgb(0x8A, 0x93, 0xA1));
            resources["SubtleIconBrush"] = Brush(Color.FromRgb(0x7A, 0x83, 0x93));
        }
    }

    private static SolidColorBrush Brush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color HslToRgb(double hue, double saturation, double lightness, double alpha = 1.0)
    {
        hue = ((hue % 360) + 360) % 360 / 360.0;
        saturation = Math.Clamp(saturation, 0.0, 1.0);
        lightness = Math.Clamp(lightness, 0.0, 1.0);

        if (saturation == 0)
        {
            var gray = (byte)Math.Round(lightness * 255);
            return Color.FromArgb((byte)Math.Round(alpha * 255), gray, gray, gray);
        }

        var q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
        var p = 2 * lightness - q;
        var r = HueToRgb(p, q, hue + 1.0 / 3.0);
        var g = HueToRgb(p, q, hue);
        var b = HueToRgb(p, q, hue - 1.0 / 3.0);
        return Color.FromArgb(
            (byte)Math.Round(alpha * 255),
            (byte)Math.Round(r * 255),
            (byte)Math.Round(g * 255),
            (byte)Math.Round(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }
}