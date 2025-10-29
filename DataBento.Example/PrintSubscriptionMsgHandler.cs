using DataBento.Net.Dbn;
using DataBento.Net.Dbn.SchemaRecords;
using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;

namespace DataBento.Example;

public class PrintSubscriptionMsgHandler : ISubscriptionMsgHandler
{
    private readonly Mbp1Reader _mdp1Reader = new();
    private readonly DatasetSubscriptionContainer _subscriptionContainer;
    private Metadata? _metadata;

    public PrintSubscriptionMsgHandler(DatasetSubscriptionContainer subscriptionContainer)
    {
        _subscriptionContainer = subscriptionContainer;
    }

    public void OnUpdate(RecordType type, ReadOnlySpan<byte> data)
    {
        switch (type)
        {
            case RecordType.Mbp1:
                OnMdp1(data);
                break;
            case RecordType.InstrumentDef:
                OnInstrumentDef(data);
                break;
            case RecordType.Internal:
                Console.WriteLine("FLUSH");
                break;
            default:
                Console.WriteLine($"Received update for {type}, data length={data.Length} bytes");
                break;
        }
    }
    private void OnInstrumentDef(ReadOnlySpan<byte> data)
    {
        if (_metadata == null)
        {
            Console.WriteLine("No metadata available to parse instrument definition");
            return;
        }
        var structData = RecordParser.SchemaRecordToObject(RecordType.InstrumentDef, data, _metadata);
        Console.WriteLine($"Received instrument definition: {structData}");
    }
    private void OnMdp1(ReadOnlySpan<byte> data)
    {
        _mdp1Reader.Initialize(data);
        _subscriptionContainer.TryGetSymbol(_mdp1Reader.InstrumentId, out var symbol);
        Console.WriteLine($"{symbol?.Symbol} {_mdp1Reader}");
    }
    public void OnMetadata(Metadata metadata)
    {
        _metadata = metadata;
        Console.WriteLine($"Received metadata: {metadata}");
    }
}