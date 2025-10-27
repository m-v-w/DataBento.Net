using DataBento.Net.Dbn;
using DataBento.Net.Dbn.StatefulReader;
using DataBento.Net.Subscriptions;
using DataBento.Net.Tcp;

namespace DataBento.Example;

public class PrintSubscriptionMsgHandler : ISubscriptionMsgHandler
{
    private readonly Mbp1Reader _mdp1Reader = new();
    private readonly DatasetSubscriptionContainer _subscriptionContainer;

    public PrintSubscriptionMsgHandler(DatasetSubscriptionContainer subscriptionContainer)
    {
        _subscriptionContainer = subscriptionContainer;
    }

    public void OnUpdate(RecordType type, ReadOnlyMemory<byte> data)
    {
        switch (type)
        {
            case RecordType.Mbp1:
                OnMdp1(data);
                break;
            default:
                Console.WriteLine($"Received update for {type}, data length={data.Length} bytes");
                break;
        }
    }
    private void OnMdp1(ReadOnlyMemory<byte> data)
    {
        _mdp1Reader.Initialize(data);
        _subscriptionContainer.TryGetSymbol(_mdp1Reader.InstrumentId, out var symbol);
        Console.WriteLine($"{symbol?.Symbol} {_mdp1Reader}");
    }
    public void OnMetadata(Metadata metadata)
    {
        Console.WriteLine($"Received metadata: {metadata}");
    }
}