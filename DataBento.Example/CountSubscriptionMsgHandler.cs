using DataBento.Net.Dbn;
using DataBento.Net.Dbn.SchemaRecords;
using DataBento.Net.Tcp;
using DataBento.Net.Utils;

namespace DataBento.Example;

public class CountSubscriptionMsgHandler : ISubscriptionMsgHandler
{
    private Metadata? _metadata;
    private long _count = 0, _lastCount = 0;
    private readonly List<TimeSpan> _latencies = new();

    public void OnUpdate(RecordType type, ReadOnlySpan<byte> data)
    {
        if (type == RecordType.Internal)
        {
            var updates = _count - _lastCount;
            _lastCount = _count;
            Console.WriteLine($"{updates} records updated, since last log.");
            return;
        }
        if (type != RecordType.Mbp1)
            return;
        _count++;
        if(_count % 1000 != 0)
            return; // only sample every 1000 records
        ref var msg = ref Mbp1Struct.UnsafeReference(data);
        var dt = TimestampUtils.UnixNanoToDateTime(msg.TsRecv);
        _latencies.Add(DateTime.UtcNow - dt);
    }
    public void OnMetadata(Metadata metadata)
    {
        _metadata = metadata;
    }
    public double[] Quantiles()
    {
        var array = _latencies.Select(x => x.TotalMilliseconds).ToArray();
        if (array.Length == 0)
            return [];
        Array.Sort(array);
        var q10 = array.Length / 10;
        var q90 = Math.Clamp(array.Length - q10 - 1, 0, array.Length - 1);
        return [array[0], array[q10], array[array.Length/2], array[q90], array[^1]];
    }
}