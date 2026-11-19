namespace Tenere.Core.Models;

public enum ClipContentType
{
    Text,
    Link,
    Code,
    Image,
    Color,
    Table,
    File
}

public enum OcrStatus
{
    NotApplicable,
    Pending,
    Processing,
    Done,
    Failed
}

public sealed class ClipItem
{
    public long Id { get; set; }
    public ClipContentType ContentType { get; set; }
    public string? Title { get; set; }
    public string? PlainText { get; set; }
    public string? Html { get; set; }
    public string? ImagePath { get; set; }
    public string? SourceApp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool Pinned { get; set; }
    public long? CollectionId { get; set; }
    public string Sensitivity { get; set; } = "normal";
    public DateTimeOffset? ExpiresAt { get; set; }
    public OcrStatus OcrStatus { get; set; }
    public string? OcrText { get; set; }
    public string ContentHash { get; set; } = string.Empty;
}