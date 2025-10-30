namespace DataBento.Net.Utils;

public sealed class AsyncAutoResetEvent
{
    private readonly Queue<TaskCompletionSource<bool>> _waits = new ();
    private bool _signaled;
    private readonly Lock _lock = new();
    
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool> tcs;
        lock (_lock)
        {
            if (_signaled)
            {
                _signaled = false;
                return;
            }
            tcs = new TaskCompletionSource<bool>();
            _waits.Enqueue(tcs);
        }
        cancellationToken.ThrowIfCancellationRequested();
        await using var handle = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        await tcs.Task;
    }
    public void Set()
    {
        TaskCompletionSource<bool>? toRelease = null;
        lock (_lock)
        {
            if (_waits.Count > 0)
                toRelease = _waits.Dequeue();
            else if (!_signaled)
                _signaled = true;
        }
        toRelease?.SetResult(true);
    }
}