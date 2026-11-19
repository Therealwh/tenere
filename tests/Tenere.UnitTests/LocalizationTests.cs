using System.Globalization;
using Tenere.Shared.Localization;
using Xunit;

namespace Tenere.UnitTests;

public class LocalizationTests
{
    [Fact]
    public void Strings_ReturnsEnglishAndRussianResources()
    {
        Strings.Culture = new CultureInfo("en");
        Assert.Equal("Settings", Strings.SettingsTitle);
        Assert.Equal("Search everything you copied…", Strings.SearchPlaceholder);

        Strings.Culture = new CultureInfo("ru");
        Assert.Equal("Настройки", Strings.SettingsTitle);
        Assert.Equal("Ищите среди скопированного…", Strings.SearchPlaceholder);

        Strings.Culture = CultureInfo.CurrentUICulture;
    }
}