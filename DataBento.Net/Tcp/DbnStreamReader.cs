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
    private const int BufferSize = 4 * 4096;
    private const int RecordHeaderLength = 16;
    private const int MetadataHeaderLength = 8;
    private readonly Pipe _pipe = new(PipeOptions.Default);
    private readonly ILogger _logger;
    private readonly Stream _stream;
    private readonly ISubscriptionHandler _subscriptionHandler;
    private readonly ISystemMsgHandler _systemMsgHandler;
    private readonly bool _leaveOpen;
    private Metadata? _metadata;
    private long _msgSeq;
    public long MsgSeq => Interlocked.Read(ref _msgSeq);
    public bool Streaming => _metadata != null;

    public DbnStreamReader(Stream stream, ISubscriptionHandler subscriptionHandler,
        ISystemMsgHandler systemMsgHandler, ILogger logger, bool leaveOpen = true)
    {
        _logger = logger;
        _leaveOpen = leaveOpen;
        _systemMsgHandler = systemMsgHandler;
        _stream = stream;
        _subscriptionHandler = subscriptionHandler;
        Debug.Assert(Marshal.SizeOf<RecordHeader>() == RecordHeaderLength);
    }

    private void ProcessRecord(in RecordHeader header, ReadOnlyMemory<byte> msg)
    {
        var recordType = (RecordType)header.RType;
        if (_metadata == null)
            throw new InvalidOperationException("Metadata must be processed before symbol mapping records");
        if (_metadata.TsOut)
            msg = msg.Slice(0, msg.Length - 8); // remove trailing timestamp
        switch (recordType)
        {
            case RecordType.System:
                HandleControlMsgResult(_systemMsgHandler.Handle(
                    RecordParser.ParseSystemMsg(_metadata, in header, msg.Span.Slice(RecordHeaderLength))));
                break;
            case RecordType.Error:
                HandleControlMsgResult(_systemMsgHandler.Handle(
                    RecordParser.ParseErrorMsg(_metadata, in header, msg.Span.Slice(RecordHeaderLength))));
                break;
            case RecordType.SymbolMapping:
                var mapping =
                    RecordParser.ParseSymbolMappingMsg(_metadata, in header, msg.Span.Slice(RecordHeaderLength));
                _logger.LogInformation("Symbol mapping {Mapping}", mapping);
                break;
            case RecordType.Mbp1:
                _subscriptionHandler.OnUpdate(recordType, msg);
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
        _subscriptionHandler.OnMetadata(_metadata);
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
        sequence.Slice(0, RecordHeaderLength).CopyTo(headerBuffer);
        ref var header = ref Unsafe.As<byte, RecordHeader>(ref MemoryMarshal.GetReference(headerBuffer));
        var totalLen = header.Length * 4;
        if (seqLen < totalLen)
            return false;
        using var owner = MemoryPool<byte>.Shared.Rent(totalLen);
        sequence.Slice(0, totalLen).CopyTo(owner.Memory.Span);
        ProcessRecord(header, owner.Memory.Slice(0, totalLen));
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

    private async Task Process()
    {
        var reader = _pipe.Reader;
        try
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buf = result.Buffer;
                while (TryProcessSeq(buf, out var pos))
                {
                    buf = buf.Slice(pos);
                }
                reader.AdvanceTo(buf.Start, buf.End);
                if (result.IsCanceled || result.IsCompleted)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing data error");
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
    public async Task Read(CancellationToken cancelToken)
    {
        var processTask = Process();
        var writer = _pipe.Writer;
        try
        {
            while (!cancelToken.IsCancellationRequested)
            {
                var memory = writer.GetMemory(BufferSize);
                // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                var c = await _stream.ReadAsync(memory, cancelToken);
                if (c == 0)
                {
                    return; // disconnected
                }

                writer.Advance(c);
                // ReSharper disable once PossiblyMistakenUseOfCancellationToken
                var flushResult = await writer.FlushAsync(cancelToken);
                if (flushResult.IsCompleted)
                {
                    throw new InvalidOperationException("Pipe completed, while writing");
                }
            }
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
            _pipe.Reset();
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