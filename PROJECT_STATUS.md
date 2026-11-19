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
- Startup load and live refresh after capture fixed.
- English and Russian resources, screenshot and demo GIF added.
- Opt-in updater with SHA-256 verification implemented.
- Unit, integration, real FlaUI UI and OCR degradation tests implemented.
- Benchmark project implemented; results in `docs/BENCHMARKS.md`.
- CI and release workflows, Inno Setup script and documentation written.
- Draft GitHub release `v0.1.0` created, not published.
- Idle memory growth fixed with conservative GC settings.

## Current

- Verifying Inno Setup installer build locally.

## Not started

- Publishing the first public release (requires explicit user approval).
- Signed MSIX (requires a signing certificate).

## Known issues

- .NET SDK is installed under `C:\Temp\dotnet`, not globally on PATH. Use the full path or set `DOTNET_ROOT`.
- Idle memory is stable but above the target: 114 MB working set, 69.5 MB private bytes versus the 25–50 MB goal.
- No OCR language packs on this machine, so accuracy on real screenshots cannot be measured here.
- Inno Setup compiler was not preinstalled; local verification is in progress.

## Last successful build

`dotnet build Tenere.slnx -c Release` — 0 warnings, 0 errors.

## Last successful test

`dotnet test Tenere.slnx -c Release --no-build` — 23 passed, 0 failed
(UnitTests 11, IntegrationTests 11, UiTests 1 real FlaUI test).

## Benchmarks

- FTS5 search over 10,000 clips: 4.752 ms mean.
- Count 10,000 clips: 1.338 ms mean.
- Three-step transformation chain: 483.5 ns mean.
- Idle memory after 30 s: 114 MB working set, 69.4 MB private bytes.

## Smoke test

Self-contained publish: 272 files, 157 MB. `Tenere.exe` starts and stays alive.

## Next step

Finish installer verification, push final changes, then await user approval before publishing the release.