using Tenere.Core.Models;

namespace Tenere.Core.Abstractions;

public sealed record ClipQuery(
    string? Text,
    ClipContentType? Type,
    DateTimeOffset? From,
    DateTimeOffset? To,
    long? CollectionId,
    bool? Pinned,
    string? Tag,
    int Limit = 200,
    int Offset = 0);

public interface IClipRepository
{
    Task InitializeAsync(CancellationToken ct);
    Task<long> AddAsync(ClipItem item, IReadOnlyDictionary<string, byte[]> extraFormats, CancellationToken ct);
    Task<ClipItem?> GetAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<ClipItem>> SearchAsync(ClipQuery query, CancellationToken ct);
    Task UpdateAsync(ClipItem item, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
    Task ClearAllAsync(CancellationToken ct);
    Task<int> PurgeExpiredAsync(CancellationToken ct);
    Task<long> CountAsync(CancellationToken ct);
}