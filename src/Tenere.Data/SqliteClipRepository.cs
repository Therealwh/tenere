using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;

namespace Tenere.Data;

/// <summary>
/// SQLite implementation of <see cref="IClipRepository"/>.
/// </summary>
public sealed class SqliteClipRepository : IClipRepository
{
    private readonly string _databasePath;

    static SqliteClipRepository()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public SqliteClipRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path is required.", nameof(databasePath));

        _databasePath = databasePath;
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    /// <summary>
    /// Creates a repository at %LOCALAPPDATA%\Tenere\tenere.db.
    /// </summary>
    public static SqliteClipRepository CreateDefault()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localAppData, "Tenere");
        return new SqliteClipRepository(Path.Combine(directory, "tenere.db"));
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await DatabaseInitializer.InitializeAsync(connection, ct);
    }

    public async Task<long> AddAsync(ClipItem item, IReadOnlyDictionary<string, byte[]> extraFormats, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);

        await using var connection = await OpenAsync(ct);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(ct);

        long id;
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO clips (
                    content_type, title, plain_text, html, image_path, source_app,
                    created_at, pinned, collection_id, sensitivity, expires_at,
                    ocr_status, ocr_text, content_hash)
                VALUES (
                    $contentType, $title, $plainText, $html, $imagePath, $sourceApp,
                    $createdAt, $pinned, $collectionId, $sensitivity, $expiresAt,
                    $ocrStatus, $ocrText, $contentHash);
                SELECT last_insert_rowid();
                """;
            BindClip(command, item);
            id = (long)(await command.ExecuteScalarAsync(ct))!;
        }

        if (extraFormats is not null)
        {
            await using var formatCommand = connection.CreateCommand();
            formatCommand.Transaction = transaction;
            formatCommand.CommandText = """
                INSERT INTO clip_formats (clip_id, format, data)
                VALUES ($clipId, $format, $data)
                ON CONFLICT(clip_id, format) DO UPDATE SET data = excluded.data;
                """;
            var clipIdParam = formatCommand.Parameters.Add("$clipId", SqliteType.Integer);
            clipIdParam.Value = id;
            var formatParam = formatCommand.Parameters.Add("$format", SqliteType.Text);
            var dataParam = formatCommand.Parameters.Add("$data", SqliteType.Blob);
            foreach (var pair in extraFormats)
            {
                formatParam.Value = pair.Key;
                dataParam.Value = pair.Value ?? Array.Empty<byte>();
                await formatCommand.ExecuteNonQueryAsync(ct);
            }
        }

        await transaction.CommitAsync(ct);
        item.Id = id;
        return id;
    }

    public async Task<ClipItem?> GetAsync(long id, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectClipSql + " WHERE c.id = $id";
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadClip(reader) : null;
    }

    public async Task<IReadOnlyList<ClipItem>> SearchAsync(ClipQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sql = new StringBuilder(SelectClipSql);
        var where = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            sql.Append(" JOIN clip_fts f ON f.rowid = c.id ");
            where.Add("clip_fts MATCH $match");
        }

        var type = query.Type;
        var from = query.From;
        var to = query.To;
        var collectionId = query.CollectionId;
        var pinned = query.Pinned;
        if (type.HasValue)
            where.Add("c.content_type = $type");
        if (from.HasValue)
            where.Add("c.created_at >= $from");
        if (to.HasValue)
            where.Add("c.created_at <= $to");
        if (collectionId.HasValue)
            where.Add("c.collection_id = $collectionId");
        if (pinned.HasValue)
            where.Add("c.pinned = $pinned");
        if (!string.IsNullOrWhiteSpace(query.Tag))
            where.Add("EXISTS (SELECT 1 FROM clip_tags ct JOIN tags t ON t.id = ct.tag_id WHERE ct.clip_id = c.id AND t.name = $tag)");

        if (where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", where));

        sql.Append(" ORDER BY c.pinned DESC, c.created_at DESC LIMIT $limit OFFSET $offset");

        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();

        if (!string.IsNullOrWhiteSpace(query.Text))
            command.Parameters.AddWithValue("$match", BuildFtsMatch(query.Text));
        if (type.HasValue)
            command.Parameters.AddWithValue("$type", (int)type.Value);
        if (from.HasValue)
            command.Parameters.AddWithValue("$from", ToStorage(from.Value));
        if (to.HasValue)
            command.Parameters.AddWithValue("$to", ToStorage(to.Value));
        if (collectionId.HasValue)
            command.Parameters.AddWithValue("$collectionId", collectionId.Value);
        if (pinned.HasValue)
            command.Parameters.AddWithValue("$pinned", pinned.Value ? 1 : 0);
        if (!string.IsNullOrWhiteSpace(query.Tag))
            command.Parameters.AddWithValue("$tag", query.Tag);
        command.Parameters.AddWithValue("$limit", Math.Clamp(query.Limit, 0, int.MaxValue));
        command.Parameters.AddWithValue("$offset", Math.Max(0, query.Offset));

        var results = new List<ClipItem>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            results.Add(ReadClip(reader));
        return results;
    }

    public async Task UpdateAsync(ClipItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);

        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE clips SET
                content_type  = $contentType,
                title         = $title,
                plain_text    = $plainText,
                html          = $html,
                image_path    = $imagePath,
                source_app    = $sourceApp,
                created_at    = $createdAt,
                pinned        = $pinned,
                collection_id = $collectionId,
                sensitivity   = $sensitivity,
                expires_at    = $expiresAt,
                ocr_status    = $ocrStatus,
                ocr_text      = $ocrText,
                content_hash  = $contentHash
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", item.Id);
        BindClip(command, item);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM clips WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task ClearAllAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM clips";
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM clips WHERE expires_at < $now AND pinned = 0";
        command.Parameters.AddWithValue("$now", ToStorage(DateTimeOffset.UtcNow));
        return await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<long> CountAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM clips";
        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task<IReadOnlyList<CollectionInfo>> GetCollectionsAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.id, c.name, COUNT(cl.id)
            FROM collections c
            LEFT JOIN clips cl ON cl.collection_id = c.id
            GROUP BY c.id, c.name
            ORDER BY c.name
            """;
        var result = new List<CollectionInfo>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(new CollectionInfo(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2)));
        return result;
    }

    public async Task<long> AddCollectionAsync(string name, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO collections(name, created_at) VALUES($name, $created); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$created", ToStorage(DateTimeOffset.UtcNow));
        return (long)(await command.ExecuteScalarAsync(ct))!;
    }

    public async Task AssignCollectionAsync(long clipId, long? collectionId, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE clips SET collection_id = $collection WHERE id = $id";
        command.Parameters.AddWithValue("$collection", collectionId.HasValue ? collectionId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$id", clipId);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AppRuleInfo>> GetAppRulesAsync(CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, app_name, COALESCE(value, ''), enabled
            FROM app_rules
            WHERE rule_type = 'transform_chain'
            ORDER BY app_name
            """;
        var result = new List<AppRuleInfo>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(new AppRuleInfo(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetInt64(3) != 0));
        return result;
    }

    public async Task SaveAppRuleAsync(string processName, string chainJson, bool enabled, CancellationToken ct)
    {
        await using var connection = await OpenAsync(ct);
        await using (var delete = connection.CreateCommand())
        {
            delete.CommandText = "DELETE FROM app_rules WHERE rule_type = 'transform_chain' AND app_name = $app";
            delete.Parameters.AddWithValue("$app", processName);
            await delete.ExecuteNonQueryAsync(ct);
        }
        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO app_rules(app_name, rule_type, value, enabled)
            VALUES($app, 'transform_chain', $value, $enabled)
            """;
        insert.Parameters.AddWithValue("$app", processName);
        insert.Parameters.AddWithValue("$value", chainJson);
        insert.Parameters.AddWithValue("$enabled", enabled ? 1 : 0);
        await insert.ExecuteNonQueryAsync(ct);
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        };
        var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(ct);
        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA recursive_triggers=ON;";
            await pragma.ExecuteNonQueryAsync(ct);
        }
        return connection;
    }

    private const string SelectClipSql = """
        SELECT c.id, c.content_type, c.title, c.plain_text, c.html, c.image_path,
               c.source_app, c.created_at, c.pinned, c.collection_id, c.sensitivity,
               c.expires_at, c.ocr_status, c.ocr_text, c.content_hash
        FROM clips c
        """;

    private static void BindClip(SqliteCommand command, ClipItem item)
    {
        command.Parameters.AddWithValue("$contentType", (int)item.ContentType);
        command.Parameters.AddWithValue("$title", (object?)item.Title ?? DBNull.Value);
        command.Parameters.AddWithValue("$plainText", (object?)item.PlainText ?? DBNull.Value);
        command.Parameters.AddWithValue("$html", (object?)item.Html ?? DBNull.Value);
        command.Parameters.AddWithValue("$imagePath", (object?)item.ImagePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$sourceApp", (object?)item.SourceApp ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", ToStorage(item.CreatedAt));
        command.Parameters.AddWithValue("$pinned", item.Pinned ? 1 : 0);
        command.Parameters.AddWithValue("$collectionId", item.CollectionId.HasValue ? item.CollectionId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$sensitivity", item.Sensitivity ?? "normal");
        command.Parameters.AddWithValue("$expiresAt", item.ExpiresAt.HasValue ? ToStorage(item.ExpiresAt.Value) : DBNull.Value);
        command.Parameters.AddWithValue("$ocrStatus", (int)item.OcrStatus);
        command.Parameters.AddWithValue("$ocrText", (object?)item.OcrText ?? DBNull.Value);
        command.Parameters.AddWithValue("$contentHash", item.ContentHash ?? string.Empty);
    }

    private static ClipItem ReadClip(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        ContentType = (ClipContentType)reader.GetInt32(1),
        Title = GetString(reader, 2),
        PlainText = GetString(reader, 3),
        Html = GetString(reader, 4),
        ImagePath = GetString(reader, 5),
        SourceApp = GetString(reader, 6),
        CreatedAt = FromStorage(reader.GetString(7)),
        Pinned = reader.GetInt64(8) != 0,
        CollectionId = reader.IsDBNull(9) ? null : reader.GetInt64(9),
        Sensitivity = GetString(reader, 10) ?? "normal",
        ExpiresAt = reader.IsDBNull(11) ? null : FromStorage(reader.GetString(11)),
        OcrStatus = (OcrStatus)reader.GetInt32(12),
        OcrText = GetString(reader, 13),
        ContentHash = GetString(reader, 14) ?? string.Empty
    };

    private static string? GetString(SqliteDataReader reader, int index) =>
        reader.IsDBNull(index) ? null : reader.GetString(index);

    private static string ToStorage(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset FromStorage(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static readonly Regex WordRegex = new(@"[\p{L}\p{N}_]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static string BuildFtsMatch(string text)
    {
        var tokens = WordRegex.Matches(text)
            .Select(m => "\"" + m.Value.Replace("\"", "\"\"") + "\"")
            .ToArray();
        return tokens.Length == 0 ? "\"\"" : string.Join(" AND ", tokens);
    }
}