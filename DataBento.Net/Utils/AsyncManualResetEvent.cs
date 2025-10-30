namespace DataBento.Net.Utils;

public sealed class AsyncManualResetEvent
{
    private volatile TaskCompletionSource<bool> _tcs = new ();

    public Task WaitAsync()
    {
        return _tcs.Task;
    }
    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var handle = cancellationToken.Register(() => _tcs.TrySetCanceled(cancellationToken));
        await _tcs.Task;
    }

    public void Set()
    {
        _tcs.TrySetResult(true);
    }

    public void Reset()
    {
        while (true)
        {
            var tcs = _tcs;
            if (!tcs.Task.IsCompleted ||
                Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                return;
        }
    }
}