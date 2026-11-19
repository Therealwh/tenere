# Architecture

## Technology decisions

| Area | Choice | Reason |
| --- | --- | --- |
| Runtime | .NET 10 LTS | Long-term support until November 2028, mature tooling. |
| UI | WPF | Fully supported on modern .NET, lower baseline memory than a web wrapper, mature virtualization and templating. |
| Materials | Win32 `DwmSetWindowAttribute` | Applies Mica/Acrylic to a WPF HWND on Windows 11; falls back on Windows 10. |
| Storage | SQLite + FTS5 via Microsoft.Data.Sqlite | Direct access, no ORM startup cost, fast full-text search. |
| OCR | Windows.Media.Ocr | Local, asynchronous, no third-party engine. |
| Clipboard | Win32 clipboard listener and `RegisterHotKey` | No low-level keyboard hooks, no input interception. |
| Installer | Inno Setup, portable ZIP, MSIX later | No signing certificate required for the first release. |

## Solution layout

```text
src/
  Tenere.App      entry point, composition root, tray lifecycle
  Tenere.UI       views, view models, styles, converters
  Tenere.Core     domain model, pipelines, abstractions
  Tenere.Data     SQLite repository, schema, FTS5, retention
  Tenere.Platform Win32 interop, clipboard, hotkeys, DWM
  Tenere.Ocr      WinRT OCR adapter
  Tenere.Updater  opt-in release check and checksum verification
  Tenere.Shared   settings, localization, logging
tests/
  Tenere.UnitTests, Tenere.IntegrationTests, Tenere.UiTests, Tenere.Benchmarks
```

Dependencies point inward: `App → UI → Core → {Data, Ocr, Platform, Updater, Shared}`. `Core` has no WPF dependency.

## Capture flow

`WM_CLIPBOARDUPDATE` → `Win32ClipboardMonitor` → `ClipboardSnapshot` → `PrivacyFilter` → `ClipCaptureService` → `IClipRepository` → FTS5 index → optional OCR queue → UI event.

The clipboard is never locked for more than 50 ms. The monitor ignores its own writes.

## Paste flow

Selection → application rule lookup → transformation chain → clipboard write → `SendInput` Ctrl+V → optional restore. Standard paste always remains available.

## Database

Tables: `clips`, `clip_formats`, `collections`, `tags`, `clip_tags`, `transform_chains`, `chain_steps`, `app_rules`, `excluded_apps`, `ocr_languages`, `settings`.

`clip_fts` is an FTS5 external-content virtual table over `clips` with triggers for insert, update and delete. Tokenizer: `unicode61 remove_diacritics 2`.

Images are stored as files under `%LOCALAPPDATA%\Tenere\images`; the database stores only the path.

## Localization

Neutral resource file is English (`Strings.resx`); Russian is `Strings.ru.resx`. All user-facing text goes through `Tenere.Shared.Localization.Strings`. The installer uses its own localized strings.

## Settings and logging

Settings live in `%LOCALAPPDATA%\Tenere\settings.json`, or next to the executable in portable mode. The schema is versioned.

Logs are rolling files capped at 1 MB × 3. They contain metadata only: clip type, size, hash, timing, error code. Clipboard text, OCR text and URLs with parameters are never logged.

## Performance targets

Idle RAM 25–50 MB, idle CPU near zero, cold start under 1.5 s, search under 100 ms on 10,000 items. Measurements come from `Tenere.Benchmarks` and manual long-run sessions.