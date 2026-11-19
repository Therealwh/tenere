# Security Policy

## Supported versions

Only the latest released version receives security fixes.

| Version | Supported |
| --- | --- |
| Latest release | Yes |
| Older releases | No |

## Reporting a vulnerability

Do not open a public issue for security problems. Use the private vulnerability reporting feature on the GitHub repository, or contact the maintainers through the profile listed on https://github.com/Therealwh.

Include: affected version, Windows build, reproduction steps, impact and any suggested mitigation. Do not include real secrets or clipboard contents.

## Response expectations

- Acknowledgement: within 7 days.
- Initial assessment: within 14 days.
- Fix and disclosure timeline: agreed with the reporter before public disclosure.

## Security model

Tenere runs fully locally. It does not send clipboard data to any service. The updater is opt-in and verifies SHA-256 checksums. Logs contain metadata only, never clipboard payloads.