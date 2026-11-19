using System.IO;
using System.Text.Json;

namespace Tenere.Shared.Settings;

public interface ISettingsService
{
    AppSettings Current { get; }
    string SettingsPath { get; }
    void Load();
    void Save();
}

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();
    public string SettingsPath { get; }

    public SettingsService(string? settingsPath = null)
    {
        SettingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tenere", "settings.json");
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
        }
        catch (JsonException)
        {
            Current = new AppSettings();
        }
        catch (IOException)
        {
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, Options));
    }
}