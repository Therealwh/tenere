namespace Tenere.Core.Models;

public sealed record ClipboardSnapshot(
    string? Text,
    string? Html,
    byte[]? ImagePng,
    IReadOnlyList<string>? Files,
    string? SourceApp);