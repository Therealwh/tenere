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
- Local Windows OCR adapter implemented.
- WPF shell with visual history, search, filters, themes and localization implemented.
- Startup load and live refresh after capture fixed.
- English and Russian resource files created.
- Opt-in updater with SHA-256 verification implemented.
- Unit, integration and real FlaUI UI tests implemented.
- Benchmark project implemented and executed.
- CI and release workflows, Inno Setup script and documentation written.
- Main-window screenshot captured and added to both READMEs.
- Draft GitHub release `v0.1.0` created, not published.

## Current

- Full solution builds and all tests pass.
- Performance numbers measured and documented.

## Not started

- Demo GIF.
- Inno Setup installer build (compiler not installed on this machine; CI workflow is ready).
- Real OCR accuracy measurement on screenshots.
- Publishing the first public release (requires explicit user approval).

## Known issues

- .NET SDK is installed under `C:\Temp\dotnet`, not globally on PATH. Use the full path or set `DOTNET_ROOT`.
- Idle memory is above target: 116 MB working set, 70.1 MB private bytes versus the 25–50 MB target. Needs optimization before public performance claims.
- Inno Setup compiler not present locally; `.iss` script has not been executed.
- No signed MSIX: a signing certificate is required.

## Last successful build

`dotnet build Tenere.slnx -c Release` — 0 warnings, 0 errors.

## Last successful test

`dotnet test Tenere.slnx -c Release --no-build` — 21 passed, 0 failed
(UnitTests 11, IntegrationTests 9, UiTests 1 real FlaUI test).

## Benchmarks

- FTS5 search over 10,000 clips: 4.752 ms mean.
- Count 10,000 clips: 1.338 ms mean.
- Three-step transformation chain: 483.5 ns mean.
- Full report: `docs/BENCHMARKS.md`.

## Smoke test

Self-contained publish: 272 files, 157 MB. `Tenere.exe` started and stayed alive, working set 116.8 MB.

## Next step

Reduce idle memory, verify installer in CI, add demo GIF, then ask user to approve publishing the release.