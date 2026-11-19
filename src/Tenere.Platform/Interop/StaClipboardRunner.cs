using System.Collections.Concurrent;

namespace Tenere.Platform.Interop;

/// <summary>
/// Runs clipboard operations on a dedicated STA thread with a bounded wait.
/// Used by the writer, which can be called from a background thread.
/// </summary>
internal static class StaClipboardRunner
{
    private static readonly Lazy<Runner> Instance = new(
        () => new Runner(),
        LazyThreadSafetyMode.ExecutionAndPublication);

    public static T? Invoke<T>(Func<T> function, int timeoutMs = 2000)
        => Instance.Value.Invoke(function, timeoutMs);

    private sealed class Runner
    {
        private readonly BlockingCollection<WorkItem> _queue = new();
        private readonly Thread _thread;

        public Runner()
        {
            _thread = new Thread(Loop)
            {
                IsBackground = true,
                Name = "Tenere.Clipboard.STA"
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        public T? Invoke<T>(Func<T> function, int timeoutMs)
        {
            using var done = new ManualResetEventSlim(false);
            T? result = default;
            Exception? failure = null;

            var item = new WorkItem(() =>
            {
                try { result = function(); }
                catch (Exception ex) { failure = ex; }
                finally { done.Set(); }
            });

            if (!_queue.TryAdd(item)) return default;
            if (!done.Wait(timeoutMs)) return default;
            if (failure is not null) throw failure;
            return result;
        }

        private void Loop()
        {
            foreach (var item in _queue.GetConsumingEnumerable())
            {
                try { item.Action(); }
                catch { }
            }
        }

        private sealed record WorkItem(Action Action);
    }
}