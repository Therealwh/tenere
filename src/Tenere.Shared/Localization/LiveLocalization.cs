using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Tenere.Shared.Localization;

/// <summary>
/// String dictionary bound through DynamicResource. Replacing it updates every open window at once.
/// </summary>
public sealed class LocalizedStringDictionary : ResourceDictionary
{
    public LocalizedStringDictionary(CultureInfo culture)
    {
        Strings.Culture = culture;
        foreach (var property in typeof(Strings).GetProperties(BindingFlags.Public | BindingFlags.Static))
        {
            if (property.Name == nameof(Strings.Culture) || property.GetMethod is null) continue;
            if (property.GetValue(null) is string value)
                Add(property.Name, value);
        }
    }
}

public static class LiveLocalization
{
    public static void Apply(ResourceDictionary resources, CultureInfo culture)
    {
        for (var i = resources.MergedDictionaries.Count - 1; i >= 0; i--)
        {
            if (resources.MergedDictionaries[i] is LocalizedStringDictionary)
                resources.MergedDictionaries.RemoveAt(i);
        }
        resources.MergedDictionaries.Add(new LocalizedStringDictionary(culture));
    }
}