# Development

## Prerequisites

- Windows 10 19045 or newer.
- .NET 10 SDK.
- Git.

```powershell
dotnet --list-sdks   # must show 10.x
```

## Build and test

```powershell
dotnet restore Tenere.slnx
dotnet build Tenere.slnx -c Release
dotnet test Tenere.slnx -c Release --no-build
```

Run the desktop app:

```powershell
dotnet run --project src/Tenere.App
```

## Test projects

- `Tenere.UnitTests` — transformations, privacy filter, pure logic.
- `Tenere.IntegrationTests` — SQLite schema, FTS5 search, retention.
- `Tenere.UiTests` — FlaUI automation against the running app.
- `Tenere.Benchmarks` — startup, search, OCR and memory scenarios.

## Design rules

- Keep `Tenere.Core` free of WPF and Win32 dependencies.
- All user-facing strings go into `Strings.resx` and `Strings.ru.resx`.
- Never log clipboard content or OCR results.
- Run tests before requesting review.