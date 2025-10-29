using System.Buffers.Binary;
using DataBento.Net.Dbn.SchemaRecords;

namespace DataBento.Net.Tcp;

public class LatencyStatistics
{
    private long _counter;
    private readonly long _sampleRate;
    private readonly List<TimeSpan> _latencies = new();

    public LatencyStatistics(long sampleRate)
    {
        _sampleRate = sampleRate;
    }

    internal void Sample(ReadOnlySpan<byte> data)
    {
        _counter++;
        if(_counter % _sampleRate != 0) 
            return;
        var ts = BinaryPrimitives.ReadUInt64LittleEndian(data);
        var dt = TimestampUtils.UnixNanoToDateTime(ts);
        _latencies.Add(DateTime.UtcNow - dt);
    }
    public TimeSpan[] Quantiles()
    {
        if (_latencies.Count == 0)
            return [];
        _latencies.Sort();
        var n = _latencies.Count;
        var q10 = n / 10;
        var q90 = Math.Clamp(n - q10 - 1, 0, n - 1);
        return [_latencies[0], _latencies[q10], _latencies[n/2], _latencies[q90], _latencies[^1]];
    }
}