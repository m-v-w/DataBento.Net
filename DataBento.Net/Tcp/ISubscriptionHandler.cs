using DataBento.Net.Dbn;

namespace DataBento.Net.Tcp;

public interface ISubscriptionHandler
{
    public void OnUpdate(RecordType type, ReadOnlyMemory<byte> data);
    public void OnMetadata(Metadata metadata);
}