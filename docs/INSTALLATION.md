# Installation

## Windows installer

1. Download `Tenere-<version>-setup.exe` from the [Releases page](https://github.com/Therealwh/tenere/releases).
2. Run the installer. Choose the interface language on the first screen.
3. Finish the wizard and select whether to launch Tenere at startup.

The installer supports English and Russian. The default follows the Windows UI culture when supported.

## Portable build

1. Download `Tenere-<version>-portable.zip`.
2. Extract it to any folder, including a USB drive.
3. Run `Tenere.exe`.

In portable mode, settings and the database are stored next to the executable.

## Requirements

- Windows 10 version 19045 or newer, or Windows 11.
- The .NET 10 runtime is included in the published build.

## Uninstall

Use Settings → Apps on Windows 11, or Control Panel on Windows 10. Portable mode is removed by deleting its folder.

## Updating

Updates are manual. Open Settings → About → Check for updates. Tenere verifies a SHA-256 checksum before running an installer.