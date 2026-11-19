# Contributing to Tenere

Thanks for helping improve Tenere.

## Ground rules

- Keep the application local-first. Do not add telemetry, accounts or cloud sync by default.
- Never persist clipboard content, OCR results or secrets in logs.
- Keep user-facing strings in resource files. English is the default language.
- Do not add heavy dependencies without a measured reason.

## Workflow

1. Open an issue before large changes so the design can be discussed.
2. Fork the repository and create a topic branch from `main`.
3. Write tests first for new logic in `Tenere.Core` and `Tenere.Data`.
4. Keep commits small and messages in English, following Conventional Commits.
5. Run the full test suite before opening a pull request:

```powershell
dotnet build Tenere.slnx -c Release
dotnet test Tenere.slnx -c Release --no-build
```

6. Fill in the pull request template and describe how you verified the change.

## Code style

- Four spaces, UTF-8, CRLF on Windows.
- One type per file. Explicit names. No single-letter variables except loop indices.
- Public APIs need XML documentation when the behavior is not obvious.

## Reporting bugs

Use the bug report template. Include Windows build, Tenere version, steps to reproduce and expected behavior. Never paste real clipboard secrets, tokens or passwords into issues.