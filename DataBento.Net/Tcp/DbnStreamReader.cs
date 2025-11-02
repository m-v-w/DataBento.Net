using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance.Buffers;
using DataBento.Net.Dbn;
using Microsoft.Extensions.Logging;

namespace DataBento.Net.Tcp;

public class DbnStreamReader : IDisposable, IAsyncDisposable
{
    private const int RecordHeaderLength = 16;
    private const int MetadataHeaderLength = 8;
    private readonly Pipe _pipe = new(PipeOptions.Default);
    private readonly ILogger _logger;
    private readonly Stream _stream;
    private readonly ISubscriptionMsgHandler _subscriptionMsgHandler;
    private readonly ISystemMsgHandler _systemMsgHandler;
    private readonly bool _leaveOpen;
    private readonly LatencyStatistics _latencyStatistics = new(100);
    private Metadata? _metadata;
    private long _msgSeq;
    public long MsgSeq => Interlocked.Read(ref _msgSeq);
    public bool Streaming => _metadata != null;
    public LatencyStatistics LatencyStatistics => _latencyStatistics;

    public DbnStreamReader(Stream stream, ISubscriptionMsgHandler subscriptionMsgHandler,
        ISystemMsgHandler systemMsgHandler, ILogger logger, bool leaveOpen = true)
    {
        _logger = logger;
        _leaveOpen = leaveOpen;
        _systemMsgHandler = systemMsgHandler;
        _stream = stream;
        _subscriptionMsgHandler = subscriptionMsgHandler;
        Debug.Assert(Marshal.SizeOf<RecordHeader>() == RecordHeaderLength);
    }
    private void ProcessRecord(in RecordHeader header, ReadOnlySpan<byte> msg)
    {
        var recordType = (RecordType)header.RType;
        if (_metadata == null)
            throw new InvalidOperationException("Metadata must be processed before symbol mapping records");
        if (_metadata.TsOut)
        {
            _latencyStatistics.Sample(msg.Slice(msg.Length - 8));
            msg = msg.Slice(0, msg.Length - 8); // remove trailing timestamp
        }
        switch (recordType)
        {
            case RecordType.System:
                HandleControlMsgResult(_systemMsgHandler.Handle(
                    RecordParser.ParseSystemMsg(_metadata, in header, msg.Slice(RecordHeaderLength))));
                break;
            case RecordType.Error:
                HandleControlMsgResult(_systemMsgHandler.Handle(
                    RecordParser.ParseErrorMsg(_metadata, in header, msg.Slice(RecordHeaderLength))));
                break;
            case RecordType.SymbolMapping:
                _systemMsgHandler.Handle(RecordParser.ParseSymbolMappingMsg(_metadata, in header,
                    msg.Slice(RecordHeaderLength)));
                break;
            case RecordType.Mbp0:
            case RecordType.Mbp1:
            case RecordType.Cmbp1:
            case RecordType.InstrumentDef:
                _subscriptionMsgHandler.OnUpdate(recordType, msg);
                break;
            default:
                _logger.LogWarning("Unknown record type {RecordType:X} len={Length}", header.RType, header.Length);
                break;
        }
    }
    private void HandleControlMsgResult(ControlMsgResult result)
    {
        switch (result)
        {
            case ControlMsgResult.None:
                break;
            case ControlMsgResult.Failed:
                _logger.LogWarning("Disconnecting, due to system message");
                Disconnect();
                break;
            default:
                throw new InvalidProgramException($"Invalid system message result {result}");
        }
    }
    private bool TryProcessMetadata(in ReadOnlySequence<byte> sequence, out SequencePosition pos)
    {
        pos = sequence.Start;
        var seqLen = sequence.Length;
        if (seqLen < MetadataHeaderLength)
            return false;
        Span<byte> headerBuffer = stackalloc byte[MetadataHeaderLength];
        sequence.Slice(0, MetadataHeaderLength).CopyTo(headerBuffer);
        if (headerBuffer[0] != (byte)'D' || headerBuffer[1] != (byte)'B' || headerBuffer[2] != (byte)'N')
            throw new DbnSerializationError("Invalid Dbn metadata header");
        var version = headerBuffer[3];
        var uLen = BinaryPrimitives.ReadUInt32LittleEndian(headerBuffer.Slice(4));
        var len = checked((int)uLen);
        if (seqLen < MetadataHeaderLength + len)
            return false;
        using var owner = SpanOwner<byte>.Allocate(len);
        sequence.Slice(MetadataHeaderLength, len).CopyTo(owner.Span);
        _metadata = MetadataParser.Parse(owner.Span.Slice(0, len), version);
        _subscriptionMsgHandler.OnMetadata(_metadata);
        pos = sequence.GetPosition(MetadataHeaderLength + len);
        return true;
    }

    private bool TryProcessRecord(in ReadOnlySequence<byte> sequence, out SequencePosition pos)
    {
        pos = sequence.Start;
        var seqLen = sequence.Length;
        if (seqLen < RecordHeaderLength)
            return false;
        Span<byte> headerBuffer = stackalloc byte[RecordHeaderLength];
        sequence.Slice(pos, RecordHeaderLength).CopyTo(headerBuffer);
        ref var header = ref Unsafe.As<byte, RecordHeader>(ref MemoryMarshal.GetReference(headerBuffer));
        var totalLen = header.Length * 4;
        if (seqLen < totalLen)
            return false;
        if (sequence.IsSingleSegment)
        {
            ProcessRecord(in header, sequence.FirstSpan.Slice(0, totalLen));
        }
        else
        {
            using var owner = SpanOwner<byte>.Allocate(totalLen);
            sequence.Slice(pos, totalLen).CopyTo(owner.Span);
            ProcessRecord(in header, owner.Span.Slice(0, totalLen));    
        }
        pos = sequence.GetPosition(totalLen);
        return true;
    }

    private bool TryProcessSeq(in ReadOnlySequence<byte> sequence, out SequencePosition pos)
    {
        Interlocked.Increment(ref _msgSeq);
        if (_metadata == null)
        {
            return TryProcessMetadata(sequence, out pos);
        }
        return TryProcessRecord(sequence, out pos);
    }

    public void PublishEmptyRecord()
    {
        _pipe.Reader.CancelPendingRead();
    }

    private void PublishRecordInternal()
    {
        _subscriptionMsgHandler.OnUpdate(RecordType.Internal, ReadOnlySpan<byte>.Empty);
    }
    
    private async Task Process()
    {
        var reader = _pipe.Reader;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                if (result.IsCanceled)
                {
                    PublishRecordInternal();
                }
                var buf = result.Buffer;
                while (TryProcessSeq(buf, out var pos))
                {
                    buf = buf.Slice(pos);
                }
                reader.AdvanceTo(buf.Start, buf.End);
                if (result.IsCompleted)
                    return;
            }
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }
    public async Task Write(byte[] spillover)
    {
        if (spillover.Length == 0)
            return;
        var writer = _pipe.Writer;
        var memory = writer.GetMemory(spillover.Length);
        spillover.AsSpan().CopyTo(memory.Span);
        writer.Advance(spillover.Length);
        await writer.FlushAsync();
    }
    public async Task Read(CancellationToken cancellationToken)
    {
        var processTask = Process();
        var writer = _pipe.Writer;
        try
        {
            await _stream.CopyToAsync(writer, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        finally
        {
            // we need to complete first, on remote disconnect, else the pipe will not be emptied
            await writer.CompleteAsync();
            await processTask;
            // leave pipe completed so it cannot be used anymore
        }
    }
    public void Disconnect()
    {
        _stream.Close();
    }
    public void Dispose()
    {
        if (!_leaveOpen)
            _stream.Dispose();
    }
    public async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
            await _stream.DisposeAsync();
    }
}