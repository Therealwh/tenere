# Changelog

All notable changes to this project are documented here. This project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Settings window with language, theme, hotkey, retention, privacy, OCR and paste options.
- Unified dark/light control styles for TextBox, ComboBox, CheckBox, Button and ScrollBar.
- Custom window chrome for the settings window.
- Global hotkey `Ctrl+Shift+V` now shows or hides the history window.
- Close button and Alt+F4 hide the app to the system tray; exit lives in the tray menu.
- Custom application icon and system tray menu in the app's visual style.
- Collections with creation, assignment and filtering.
- Copy action: copy any clip back to the clipboard with the card button or a double-click.
- Per-application paste rules backed by SQLite and the transformation pipeline.
- Per-monitor DPI awareness manifest.
- Initial solution structure with .NET 10 LTS and WPF.
- Clipboard capture pipeline with privacy filtering and deduplication by content hash.
- SQLite storage with FTS5 full-text search, prefix matching while typing and substring fallback.
- Local OCR adapter using Windows Media OCR.
- Smart paste transformation pipeline.
- English and Russian localization resources with live switching via `DynamicResource` — no restart needed.
- Unit, integration, UI and benchmark test projects.
- CI and release workflows, Inno Setup installer script.

### Changed

- Nothing yet.

### Fixed

- Nothing yet.