using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Tenere.Core.Abstractions;
using Tenere.Core.Models;
using Tenere.Core.Services;
using Tenere.Data;

BenchmarkRunner.Run<ClipboardBenchmarks>();

[MemoryDiagnoser]
public class ClipboardBenchmarks
{
    private const int ItemCount = 10_000;
    private IClipRepository _repository = null!;
    private readonly TransformPipeline _pipeline = new();
    private readonly TransformSpec[] _chain =
    [
        new("trim"),
        new("strip-url-params"),
        new("normalize-line-endings")
    ];
    private string _databasePath = string.Empty;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"tenere-bench-{Guid.NewGuid():N}.db");
        _repository = new SqliteClipRepository(_databasePath);
        await _repository.InitializeAsync(CancellationToken.None);

        for (var i = 0; i < ItemCount; i++)
        {
            var text = i % 10 == 0
                ? $"Release notes for Tenere build {i} https://example.com/page?utm_source=bench&id={i}"
                : $"Clip item number {i} with searchable content and notes";
            await _repository.AddAsync(new ClipItem
            {
                ContentType = ClipContentType.Text,
                Title = $"Item {i}",
                PlainText = text,
                SourceApp = "benchmark",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-i),
                ContentHash = $"hash-{i}",
                OcrStatus = OcrStatus.NotApplicable
            }, new Dictionary<string, byte[]>(), CancellationToken.None);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            File.Delete(_databasePath);
            File.Delete(_databasePath + "-wal");
            File.Delete(_databasePath + "-shm");
        }
        catch (IOException) { }
    }

    [Benchmark(Description = "FTS search across 10k clips")]
    public async Task<int> SearchByText() =>
        (await _repository.SearchAsync(
            new ClipQuery("searchable", null, null, null, null, null, null, 50),
            CancellationToken.None)).Count;

    [Benchmark(Description = "Count 10k clips")]
    public async Task<long> Count() =>
        await _repository.CountAsync(CancellationToken.None);

    [Benchmark(Description = "Transformation chain")]
    public string Transform() =>
        _pipeline.Apply("  https://example.com/page?utm_source=mail  \r\nsecond line  ", _chain);
}