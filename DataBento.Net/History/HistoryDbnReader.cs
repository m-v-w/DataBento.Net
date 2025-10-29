using DataBento.Net.Dbn;
using DataBento.Net.Tcp;
using Microsoft.Extensions.Logging;

namespace DataBento.Net.History;

public class HistoryDbnReader : ISubscriptionMsgHandler, ISystemMsgHandler, IDisposable, IAsyncDisposable
{
    private readonly DbnStreamReader _reader;
    private readonly ILogger _logger;
    public Metadata? Metadata { get; private set; }
    public List<object> Records { get; } = new ();

    public HistoryDbnReader(Stream stream, ILogger logger)
    {
        _logger = logger;
        _reader = new DbnStreamReader(stream, this, this, logger);
    }

    public async Task Read(CancellationToken cancellationToken)
    {
        await _reader.Read(cancellationToken);
    }

    public void OnUpdate(RecordType type, ReadOnlySpan<byte> data)
    {
        Records.Add(RecordParser.SchemaRecordToObject(type, data, Metadata ?? throw new InvalidProgramException("Metadata not set")));
    }
    public void OnMetadata(Metadata metadata)
    {
        Metadata = metadata;
    }

    public ControlMsgResult Handle(SystemMessage systemMsg)
    {
        throw new NotSupportedException("System messages are not supported in history reader");
    }

    public ControlMsgResult Handle(ErrorMessage errorMsg)
    {
        throw new DbnSerializationError($"Error message {errorMsg.Error}");
    }

    public void Handle(SymbolMappingMsg symbolMappingMsg)
    {
        _logger.LogInformation("Symbol Mapping: {Mapping}", symbolMappingMsg);
    }

    public void Dispose()
    {
        _reader.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _reader.DisposeAsync();
    }
}