using System.Text;
using Microsoft.Data.Sqlite;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;
using Tenere.Data;

namespace Tenere.IntegrationTests;

public sealed class SqliteClipRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _databasePath;
    private readonly SqliteClipRepository _repository;

    public SqliteClipRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "tenere-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _databasePath = Path.Combine(_directory, "tenere.db");
        _repository = new SqliteClipRepository(_databasePath);
        _repository.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Files may still be held by SQLite; temp directory is negligible.
        }
    }

    [Fact]
    public async Task Add_Get_RoundtripsAllFields()
    {
        var item = NewClip("hello world", ClipContentType.Text);
        item.Title = "Greeting";
        item.Html = "<b>hello</b>";
        item.ImagePath = "C:\\img.png";
        item.SourceApp = "Notepad";
        item.Pinned = true;
        item.CollectionId = 42;
        item.Sensitivity = "high";
        item.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1);
        item.OcrStatus = OcrStatus.Done;
        item.OcrText = "recognized";
        item.ContentHash = "hash-1";

        await using (var setup = new SqliteConnection($"Data Source={_databasePath}"))
        {
            await setup.OpenAsync();
            await using var insert = setup.CreateCommand();
            insert.CommandText = "INSERT INTO collections(id, name, created_at) VALUES (42, 'Work', $createdAt)";
            insert.Parameters.AddWithValue("$createdAt", DateTimeOffset.UtcNow.ToString("O"));
            await insert.ExecuteNonQueryAsync();
        }

        var id = await _repository.AddAsync(item, new Dictionary<string, byte[]>
        {
            ["text/html"] = Encoding.UTF8.GetBytes("<b>hello</b>")
        }, CancellationToken.None);
        Assert.True(id > 0);
        Assert.Equal(id, item.Id);

        var loaded = await _repository.GetAsync(id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(item.Title, loaded.Title);
        Assert.Equal(item.PlainText, loaded.PlainText);
        Assert.Equal(item.Html, loaded.Html);
        Assert.Equal(item.ImagePath, loaded.ImagePath);
        Assert.Equal(item.SourceApp, loaded.SourceApp);
        Assert.Equal(item.CreatedAt.ToUniversalTime(), loaded.CreatedAt.ToUniversalTime());
        Assert.True(loaded.Pinned);
        Assert.Equal(42, loaded.CollectionId);
        Assert.Equal("high", loaded.Sensitivity);
        Assert.Equal(item.ExpiresAt!.Value.ToUniversalTime(), loaded.ExpiresAt!.Value.ToUniversalTime());
        Assert.Equal(OcrStatus.Done, loaded.OcrStatus);
        Assert.Equal("recognized", loaded.OcrText);
        Assert.Equal("hash-1", loaded.ContentHash);

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT data FROM clip_formats WHERE clip_id = $id AND format = 'text/html'";
        command.Parameters.AddWithValue("$id", id);
        var blob = (byte[])(await command.ExecuteScalarAsync())!;
        Assert.Equal("<b>hello</b>", Encoding.UTF8.GetString(blob));
    }

    [Fact]
    public async Task Search_FtsFindsEnglishAndRussianText()
    {
        await _repository.AddAsync(NewClip("The quick brown fox", ClipContentType.Text), EmptyFormats, CancellationToken.None);
        await _repository.AddAsync(NewClip("Привет мир, это тест", ClipContentType.Text), EmptyFormats, CancellationToken.None);

        var english = await _repository.SearchAsync(new ClipQuery("quick", null, null, null, null, null, null), CancellationToken.None);
        Assert.Single(english);
        Assert.Contains("quick", english[0].PlainText, StringComparison.Ordinal);

        var russian = await _repository.SearchAsync(new ClipQuery("привет", null, null, null, null, null, null), CancellationToken.None);
        Assert.Single(russian);
        Assert.Contains("Привет", russian[0].PlainText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Search_AppliesFilters_AndOrdersByPinnedThenDate()
    {
        var older = NewClip("alpha beta", ClipContentType.Text);
        older.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        await _repository.AddAsync(older, EmptyFormats, CancellationToken.None);

        var newer = NewClip("alpha beta", ClipContentType.Code);
        newer.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        newer.Pinned = true;
        await _repository.AddAsync(newer, EmptyFormats, CancellationToken.None);

        var all = await _repository.SearchAsync(new ClipQuery("alpha", null, null, null, null, null, null), CancellationToken.None);
        Assert.Equal(2, all.Count);
        Assert.True(all[0].Pinned);

        var byType = await _repository.SearchAsync(new ClipQuery("alpha", ClipContentType.Code, null, null, null, null, null), CancellationToken.None);
        Assert.Single(byType);
        Assert.Equal(ClipContentType.Code, byType[0].ContentType);

        var byPinned = await _repository.SearchAsync(new ClipQuery("alpha", null, null, null, null, true, null), CancellationToken.None);
        Assert.Single(byPinned);
        Assert.True(byPinned[0].Pinned);

        var from = DateTimeOffset.UtcNow.AddMinutes(-7);
        var byFrom = await _repository.SearchAsync(new ClipQuery("alpha", null, from, null, null, null, null), CancellationToken.None);
        Assert.Single(byFrom);
        Assert.Equal(newer.Id, byFrom[0].Id);

        var paged = await _repository.SearchAsync(new ClipQuery("alpha", null, null, null, null, null, null, 1, 1), CancellationToken.None);
        Assert.Single(paged);
        Assert.Equal(older.Id, paged[0].Id);
    }

    [Fact]
    public async Task Search_ByTag_UsesFtSynchronizedTags()
    {
        var id = await _repository.AddAsync(NewClip("tagged content", ClipContentType.Text), EmptyFormats, CancellationToken.None);
        await AddTagAsync(id, "важное");

        var byTag = await _repository.SearchAsync(new ClipQuery(null, null, null, null, null, null, "важное"), CancellationToken.None);
        Assert.Single(byTag);
        Assert.Equal(id, byTag[0].Id);

        var byFtsTag = await _repository.SearchAsync(new ClipQuery("важное", null, null, null, null, null, null), CancellationToken.None);
        Assert.Single(byFtsTag);
    }

    [Fact]
    public async Task Update_ChangesOcrText_AndMakesItSearchable()
    {
        var id = await _repository.AddAsync(NewClip("original", ClipContentType.Image), EmptyFormats, CancellationToken.None);
        var item = (await _repository.GetAsync(id, CancellationToken.None))!;
        item.OcrStatus = OcrStatus.Done;
        item.OcrText = "распознанный текст";
        item.Title = "Updated title";
        await _repository.UpdateAsync(item, CancellationToken.None);

        var updated = await _repository.GetAsync(id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(OcrStatus.Done, updated.OcrStatus);
        Assert.Equal("распознанный текст", updated.OcrText);
        Assert.Equal("Updated title", updated.Title);

        var found = await _repository.SearchAsync(new ClipQuery("распознанный", null, null, null, null, null, null), CancellationToken.None);
        Assert.Single(found);
    }

    [Fact]
    public async Task PurgeExpired_DeletesUnpinnedExpiredOnly()
    {
        var expired = NewClip("expired", ClipContentType.Text);
        expired.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await _repository.AddAsync(expired, EmptyFormats, CancellationToken.None);

        var pinned = NewClip("pinned", ClipContentType.Text);
        pinned.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        pinned.Pinned = true;
        await _repository.AddAsync(pinned, EmptyFormats, CancellationToken.None);

        var fresh = NewClip("fresh", ClipContentType.Text);
        fresh.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(1);
        await _repository.AddAsync(fresh, EmptyFormats, CancellationToken.None);

        var removed = await _repository.PurgeExpiredAsync(CancellationToken.None);
        Assert.Equal(1, removed);
        Assert.Equal(2, await _repository.CountAsync(CancellationToken.None));
        Assert.Null(await _repository.GetAsync(expired.Id, CancellationToken.None));
        Assert.NotNull(await _repository.GetAsync(pinned.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_And_ClearAll_RemoveRows()
    {
        var first = await _repository.AddAsync(NewClip("first", ClipContentType.Text), EmptyFormats, CancellationToken.None);
        var second = await _repository.AddAsync(NewClip("second", ClipContentType.Text), EmptyFormats, CancellationToken.None);
        Assert.Equal(2, await _repository.CountAsync(CancellationToken.None));

        await _repository.DeleteAsync(first, CancellationToken.None);
        Assert.Equal(1, await _repository.CountAsync(CancellationToken.None));
        Assert.Null(await _repository.GetAsync(first, CancellationToken.None));

        await _repository.ClearAllAsync(CancellationToken.None);
        Assert.Equal(0, await _repository.CountAsync(CancellationToken.None));
        Assert.Null(await _repository.GetAsync(second, CancellationToken.None));
    }

    [Fact]
    public async Task FtsIndex_StaysConsistentAfterMutations()
    {
        var first = await _repository.AddAsync(NewClip("consistency alpha", ClipContentType.Text), EmptyFormats, CancellationToken.None);
        var second = await _repository.AddAsync(NewClip("consistency beta", ClipContentType.Text), EmptyFormats, CancellationToken.None);
        await AddTagAsync(second, "tag-one");

        var item = (await _repository.GetAsync(first, CancellationToken.None))!;
        item.OcrText = "consistency gamma";
        await _repository.UpdateAsync(item, CancellationToken.None);
        await _repository.DeleteAsync(second, CancellationToken.None);
        await _repository.ClearAllAsync(CancellationToken.None);

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var check = connection.CreateCommand();
        check.CommandText = "INSERT INTO clip_fts(clip_fts) VALUES('integrity-check')";
        await check.ExecuteNonQueryAsync();

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM clip_fts WHERE clip_fts MATCH 'consistency'";
        Assert.Equal(0L, (long)(await countCommand.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Initialize_IsIdempotent()
    {
        await _repository.InitializeAsync(CancellationToken.None);
        Assert.Equal(0, await _repository.CountAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Search_MatchesWordPrefix()
    {
        await _repository.AddAsync(NewClip("Tenere remembers everything you copy", ClipContentType.Text),
            EmptyFormats, CancellationToken.None);

        var results = await _repository.SearchAsync(
            new ClipQuery("Te", null, null, null, null, null, null), CancellationToken.None);

        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task Search_MatchesSubstringInsideWord()
    {
        await _repository.AddAsync(NewClip("Tenere remembers everything you copy", ClipContentType.Text),
            EmptyFormats, CancellationToken.None);

        var results = await _repository.SearchAsync(
            new ClipQuery("mem", null, null, null, null, null, null), CancellationToken.None);

        Assert.NotEmpty(results);
    }

    private static IReadOnlyDictionary<string, byte[]> EmptyFormats =>
        new Dictionary<string, byte[]>();

    private static ClipItem NewClip(string text, ClipContentType type) => new()
    {
        ContentType = type,
        PlainText = text,
        CreatedAt = DateTimeOffset.UtcNow,
        ContentHash = Guid.NewGuid().ToString("N")
    };

    private async Task AddTagAsync(long clipId, string tagName)
    {
        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO tags(name) VALUES ($name);
            INSERT OR IGNORE INTO clip_tags(clip_id, tag_id)
            SELECT $clipId, id FROM tags WHERE name = $name;
            """;
        command.Parameters.AddWithValue("$name", tagName);
        command.Parameters.AddWithValue("$clipId", clipId);
        await command.ExecuteNonQueryAsync();
    }
}