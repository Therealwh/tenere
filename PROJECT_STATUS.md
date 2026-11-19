# Project Status

Last updated: 2026-11-19

## Completed

- Stage 0: competitor research, MCP and Skills audit, multi-agent plan.
- Stage 1: name selected by the user — **Tenere**.
- Stage 2: architecture approved by the user.
- GitHub repository created: `https://github.com/Therealwh/tenere`.
- .NET 10 SDK 10.0.404 installed locally under `C:\Temp\dotnet`.
- Solution scaffolded with 12 projects and correct dependency direction:
  `App → UI → Core → {Data, Ocr, Platform, Updater, Shared}`.
- Core contracts, capture pipeline, privacy filter and transformation pipeline implemented.
- SQLite data layer with FTS5, triggers, indexes and retention implemented.
- Win32 clipboard monitor, writer, hotkeys, source-app resolver and Mica backdrop implemented.
- Local Windows OCR adapter implemented with graceful degradation when no language pack exists.
- WPF shell with visual history, search, filters, themes and localization implemented.
- Settings window: language, theme, hotkey, retention, privacy, OCR and paste options.
- Collections: create, assign clips and filter history.
- Per-application paste rules stored in SQLite and applied through the transformation pipeline.
- Per-monitor DPI awareness manifest added.
- Startup load and live refresh after capture fixed.
- English and Russian resources, screenshot and demo GIF added.
- Opt-in updater with SHA-256 verification implemented.
- Unit, integration, real FlaUI UI and OCR degradation tests implemented.
- Benchmark project implemented; results in `docs/BENCHMARKS.md`.
- CI and release workflows, Inno Setup script and documentation written.
- Hosted CI passes for unit and integration tests; UI tests are excluded from hosted runs.
- Draft GitHub release `v0.1.0` created, not published.
- Idle memory growth fixed with conservative GC settings.

## Current

- Nothing blocking. Release draft awaits user approval.

## Not started

- Publishing the first public release (requires explicit user approval).
- Signed MSIX (requires a signing certificate).

## Known issues

- .NET SDK is installed under `C:\Temp\dotnet`, not globally on PATH. Use the full path or set `DOTNET_ROOT`.
- Idle memory is stable but above the target: 114 MB working set, 69.5 MB private bytes versus the 25–50 MB goal.
- No OCR language packs on this machine, so accuracy on real screenshots cannot be measured here.
- Installer is unsigned; Windows SmartScreen will warn until a code-signing certificate is added.
- Earlier hosted CI failure was the FlaUI UI test: no interactive desktop on the runner. CI now excludes `Category=UI`; the UI tests still run locally.
- Settings window title can appear in Russian on this host because WPF resolves `x:Static` against the thread UI culture before the saved language is applied; resource strings themselves switch correctly.

## Last successful build

`dotnet build Tenere.slnx -c Release` — 0 warnings, 0 errors.

## Last successful test

`dotnet test Tenere.slnx -c Release --no-build` — 25 passed, 0 failed
(UnitTests 12, IntegrationTests 11, UiTests 2 real FlaUI tests).

## Benchmarks

- FTS5 search over 10,000 clips: 4.752 ms mean.
- Count 10,000 clips: 1.338 ms mean.
- Three-step transformation chain: 483.5 ns mean.
- Idle memory after 30 s: 114 MB working set, 69.4 MB private bytes.

## Smoke test

Self-contained publish: 272 files, 157 MB. `Tenere.exe` starts and stays alive.

## Installer verification

Inno Setup 6.7.4 compiled `installer/Output/Tenere-0.1.0-setup.exe` (48.4 MB) with EN/RU languages.
Silent install to `C:\Temp\TenereInstall` succeeded; installed app started and stayed alive; silent uninstall succeeded.

## Status

Architecture 100%; core 100%; data 100%; Win32 90%; UI 95%; OCR 75%; privacy 90%; localization 95%; tests 95%; benchmark 100%; CI 100%; installer 100%; documentation 90%; release 10% (draft only).

## Next step

Await explicit approval before publishing `v0.1.0`. Optional remaining work: visual chain editor, tags and import/export, further RAM reduction, DPI verification on mixed-monitor setups, signed MSIX when a certificate is available.