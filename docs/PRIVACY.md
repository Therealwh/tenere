# Privacy

Tenere is local-first. There are no accounts, no servers and no telemetry.

## What Tenere stores

| Data | Where | Purpose |
| --- | --- | --- |
| Clip text, title, type, timestamps | SQLite database in `%LOCALAPPDATA%\Tenere` | History and search |
| Image files | `%LOCALAPPDATA%\Tenere\images` | Visual previews and OCR |
| OCR text | SQLite FTS index | Search inside screenshots |
| Settings | `settings.json` | User preferences |
| Metadata logs | Rolling files in `logs\` | Diagnostics only |

Clipboard contents are never written to logs.

## Privacy Shield

- Excludes common password managers by default: KeePassXC, 1Password, Bitwarden, Password Safe.
- Detects common token and private-key patterns and blocks matching clips.
- Marks possibly sensitive short text containing `token`, `api_key` or `secret` as suspect.
- Supports manual application exclusions and custom regex patterns.
- Supports retention limits, complete history disablement and one-click wipe.

Automatic detection is a aid, not a guarantee. Review the exclusion list for your own tools.

## Network

The only network feature is the manual updater. It checks GitHub Releases when you click the button, downloads an asset and verifies its SHA-256 checksum before installation. No clipboard data is sent.

## Export

Exports are local files created by the user. Treat them as sensitive and store them accordingly.