using System.IO;
using Microsoft.Extensions.Logging;

namespace Tenere.Shared.Logging;

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly object _gate = new();
    private StreamWriter? _writer;
    private long _length;

    public FileLoggerProvider(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tenere", "logs");
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose()
    {
        lock (_gate)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void Write(string message)
    {
        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                var path = Path.Combine(_directory, "tenere.log");
                if (_writer is null || _length > 1024 * 1024)
                {
                    _writer?.Dispose();
                    RotateIfNeeded(path);
                    _writer = new StreamWriter(path, append: true) { AutoFlush = true };
                    _length = new FileInfo(path).Length;
                }
                var safe = Sanitize(message);
                _writer.WriteLine($"{DateTimeOffset.UtcNow:O} {safe}");
                _length += safe.Length + 1;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static void RotateIfNeeded(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length <= 1024 * 1024) return;
        for (var i = 2; i >= 1; i--)
        {
            var source = i == 1 ? path : path + $".{i - 1}";
            var target = path + $".{i}";
            if (File.Exists(source)) File.Copy(source, target, true);
        }
        File.Delete(path);
    }

    private static string Sanitize(string message)
    {
        // Never persist clipboard payloads, OCR results, tokens or URLs with parameters.
        var value = message.Replace("\r", " ").Replace("\n", " ");
        return value.Length > 512 ? value[..512] : value;
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            provider.Write($"{logLevel} [{category}] {formatter(state, exception)}");
        }
    }
}