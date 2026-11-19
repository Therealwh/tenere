<div align="center">

# Tenere

**Your memory for everything you copy.**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![CI](https://github.com/Therealwh/tenere/actions/workflows/ci.yml/badge.svg)](https://github.com/Therealwh/tenere/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/Therealwh/tenere)](https://github.com/Therealwh/tenere/releases)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://www.microsoft.com/windows)

[English](README.md) · [Русский](README.ru.md)

</div>

Tenere is a local-first clipboard manager for Windows. It keeps everything you copy, shows it as a visual history, finds text inside old screenshots with local OCR, transforms content before pasting, and keeps secrets out of your history by default.

No account. No cloud. No telemetry.

## Features

- **Clipboard history** — text, links, code, images, tables, colors and files, with source app and timestamps.
- **Visual memory** — switch between a compact list and a card grid with previews.
- **Instant search** — full-text search across plain text, OCR text, source apps, tags and dates.
- **Local OCR** — Windows Media OCR recognizes text inside copied images. Nothing leaves the device.
- **Smart paste** — chain transformations: strip formatting, clean URLs, prettify JSON, convert tables to Markdown, apply your own regex.
- **Context-aware paste** — per-application paste rules. Keep formatting in Word, paste plain text in a terminal.
- **Collections** — pin, tag, organize, import and export your clips.
- **Privacy Shield** — excludes password managers out of the box, detects tokens and keys, supports manual exclusions, retention limits and one-click wipe.

## Why Tenere

- **Private by design.** History never leaves your machine. Clipboard content is never written to logs.
- **Light.** Target idle memory is 25–50 MB with near-zero CPU. Measured by a real benchmark, not marketing numbers.
- **Native.** Built with C# and WPF on .NET 10, not an Electron wrapper.
- **Fast.** A global hotkey opens the history instantly; search results appear as you type.

## Installation

Download the installer or portable build from the [Releases page](https://github.com/Therealwh/tenere/releases).

```powershell
# Portable (example)
Expand-Archive tenere-portable.zip -DestinationPath C:\Tools\Tenere
.\C:\Tools\Tenere\Tenere.exe
```

System requirements: Windows 10 version 19045 or newer, Windows 11, .NET 10 runtime included in the installer.

See [docs/INSTALLATION.md](docs/INSTALLATION.md) for details.

## Privacy

Tenere works fully offline. It does not create accounts, send clipboard data anywhere, or enable telemetry. The updater runs only when you click "Check for updates" and verifies a SHA-256 checksum before installation.

Read [docs/PRIVACY.md](docs/PRIVACY.md) for the full privacy model.

## Technology

C# · .NET 10 LTS · WPF · SQLite with FTS5 · Windows Media OCR · Win32 clipboard APIs · Inno Setup

## Roadmap

See [ROADMAP.md](ROADMAP.md) and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

[MIT](LICENSE)

> Screenshots and a demo GIF will be added with the first public release.