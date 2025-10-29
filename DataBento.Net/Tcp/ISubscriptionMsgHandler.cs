using DataBento.Net.Dbn;

namespace DataBento.Net.Tcp;

public interface ISubscriptionMsgHandler
{
    public void OnUpdate(RecordType type, ReadOnlySpan<byte> data);
    public void OnMetadata(Metadata metadata);
}