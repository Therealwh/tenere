namespace Tenere.Shared.Settings;

public sealed class AppSettings
{
    public GeneralSettings General { get; set; } = new();
    public HistorySettings History { get; set; } = new();
    public PrivacySettings Privacy { get; set; } = new();
    public OcrSettings Ocr { get; set; } = new();
    public PasteSettings Paste { get; set; } = new();
    public AppearanceSettings Appearance { get; set; } = new();
    public int SchemaVersion { get; set; } = 1;
}

public sealed class GeneralSettings
{
    public string Language { get; set; } = "en";
    public bool Autostart { get; set; }
    public string HotkeyToggle { get; set; } = "Ctrl+Shift+V";
    public string HotkeyPastePlain { get; set; } = "Ctrl+Alt+V";
    public bool StartMinimized { get; set; } = true;
}

public sealed class HistorySettings
{
    public int RetentionDays { get; set; } = 30;
    public int MaxItems { get; set; } = 5000;
    public bool Deduplicate { get; set; } = true;
    public bool CaptureImages { get; set; } = true;
    public bool CaptureLinks { get; set; } = true;
}

public sealed class PrivacySettings
{
    public bool ShieldEnabled { get; set; } = true;
    public bool DisableHistory { get; set; }
    public List<string> ExcludedApps { get; set; } = ["KeePassXC", "1Password", "Bitwarden", "Password Safe"];
    public List<string> SecretPatterns { get; set; } =
    [
        @"AKIA[0-9A-Z]{16}",
        @"ghp_[A-Za-z0-9]{36}",
        @"-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----",
        @"(?i)password\s*[:=]\s*\S{6,}"
    ];
}

public sealed class OcrSettings
{
    public bool Enabled { get; set; } = true;
    public List<string> Languages { get; set; } = ["en", "ru"];
    public bool IndexOcrText { get; set; } = true;
}

public sealed class PasteSettings
{
    public bool RestoreClipboardAfterPaste { get; set; }
    public bool PlainTextByDefault { get; set; }
}

public sealed class AppearanceSettings
{
    public string Theme { get; set; } = "system";
    public bool UseMica { get; set; } = true;
    public string ViewMode { get; set; } = "grid";
    public double AccentHue { get; set; } = 212;
}