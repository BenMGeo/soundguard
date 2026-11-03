using System.Collections.Concurrent;

namespace SoundGuard;

/// <summary>Runs actions one at a time on a dedicated thread so COM callbacks never block.</summary>
public sealed class SerialQueue : IDisposable
{
    private readonly BlockingCollection<Action> _items = new();
    private readonly Thread _thread;

    public SerialQueue(Action<Exception> onError)
    {
        _thread = new Thread(() =>
        {
            foreach (var a in _items.GetConsumingEnumerable())
            {
                try { a(); } catch (Exception ex) { onError(ex); }
            }
        }) { IsBackground = true, Name = "SoundGuard worker" };
        _thread.Start();
    }

    public void Post(Action a)
    {
        try { _items.Add(a); }
        catch (InvalidOperationException) { /* already shutting down */ }
    }

    /// <summary>Waits for queued work to finish, then stops the worker.</summary>
    public void Dispose()
    {
        _items.CompleteAdding();
        _thread.Join(TimeSpan.FromSeconds(3));
    }
}
