using DataBento.Net.Dbn;
using DataBento.Net.Dbn.StatefulReader;
using DataBento.Net.Tcp;

namespace DataBento.Example;

public class PrintSubscriptionHandler : ISubscriptionHandler
{
    private readonly Mbp1Reader _mdp1Reader = new();
    public void OnUpdate(RecordType type, ReadOnlyMemory<byte> data)
    {
        switch (type)
        {
            case RecordType.Mbp1:
                _mdp1Reader.Initialize(data);
                Console.WriteLine(_mdp1Reader.ToString());
                break;
            default:
                Console.WriteLine($"Received update for {type}, data length={data.Length} bytes");
                break;
        }
    }

    public void OnMetadata(Metadata metadata)
    {
        Console.WriteLine($"Received metadata: {metadata}");
    }
}