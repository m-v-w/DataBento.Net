namespace DataBento.Net.Dbn;

public record SystemMessage(ushort PublisherId, uint InstrumentId, string Message, byte? Code)
{
    internal SystemMessage(in RecordHeader header, string message, byte? code) 
        : this(header.PublisherId, header.InstrumentId, message, code)
    {
    }
}
public enum SystemMessageCode : byte
{
    Heartbeat = 0,
    SubscriptionAck = 1,
    SlowReaderWarning = 2,
    ReplyCompleted = 3,
    EndOfInterval = 4
}
public record ErrorMessage(ushort PublisherId, uint InstrumentId, string Error, byte? Code, bool? IsLast)
{
    internal ErrorMessage(in RecordHeader header, string message, byte? code, bool? isLast) 
        : this(header.PublisherId, header.InstrumentId, message, code, isLast)
    {
    }
}