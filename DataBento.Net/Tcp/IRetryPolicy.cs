namespace DataBento.Net.Tcp;

public interface IRetryPolicy
{
    /// <summary>
    /// Determines the delay before the next retry attempt.
    /// Return null to indicate no further retries should be attempted.
    /// </summary>
    TimeSpan? NextRetryDelay(long previousRetryCount);
}
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _initialDelay;
    private readonly double _exponentialFactor;
    private readonly TimeSpan? _maxDelay;
    public ExponentialBackoffRetryPolicy(TimeSpan initialDelay, double exponentialFactor=2.0, TimeSpan? maxDelay=null)
    {
        _initialDelay = initialDelay;
        _exponentialFactor = exponentialFactor;
        _maxDelay = maxDelay;
    }
    public TimeSpan? NextRetryDelay(long previousRetryCount)
    {
        var multiplier = Math.Pow(_exponentialFactor, previousRetryCount);
        var delay = _initialDelay.Multiply(multiplier);
        if (_maxDelay.HasValue && delay > _maxDelay.Value)
            return _maxDelay.Value;
        return delay;
    }
}
public class NoRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(long previousRetryCount) => null;
}
