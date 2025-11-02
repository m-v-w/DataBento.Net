using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using DataBento.Net.Dbn.SchemaRecords;

namespace DataBento.Net.Dbn;

public static class RecordParser
{
    public static object SchemaRecordToObject(RecordType type, ReadOnlySpan<byte> span, Metadata metadata)
    {
        switch (type)
        {
            case RecordType.InstrumentDef:
                return metadata.Version switch
                {
                    1 => InstrumentDefStructV1.UnsafeReference(span),
                    _ => throw new NotSupportedException($"InstrumentDef not supported in version {metadata.Version}")
                };
            case RecordType.Mbp1:
            case RecordType.Cmbp1:
                return Mbp1Struct.UnsafeReference(span);
            case RecordType.Mbp0:
                return TradesStruct.UnsafeReference(span);
            default:
                throw new NotSupportedException($"unsupported type {type}");
        }
    }
    
    
    internal static SystemMessage ParseSystemMsg(Metadata metadata, in RecordHeader header, ReadOnlySpan<byte> body)
    {
        if (metadata.Version == 1)
        {
            var v1Pos = 0;
            var v1Msg = MetadataParser.ReadFixLenString(64, body, ref v1Pos);
            if(v1Pos != body.Length)
                throw new DbnSerializationError("Did not consume all data");
            return new SystemMessage(in header, v1Msg, null);
        }
        var pos = 0;
        var msg = MetadataParser.ReadFixLenString(303, body, ref pos);
        var code = body[pos++];
        if(pos != body.Length)
            throw new DbnSerializationError("Did not consume all data");
        return new SystemMessage(in header, msg, code);
    }
    internal static ErrorMessage ParseErrorMsg(Metadata metadata, in RecordHeader header, ReadOnlySpan<byte> body)
    {
        if (metadata.Version == 1)
        {
            var v1Pos = 0;
            var v1Msg = MetadataParser.ReadFixLenString(64, body, ref v1Pos);
            if(v1Pos != body.Length)
                throw new DbnSerializationError("Did not consume all data");
            return new ErrorMessage(in header, v1Msg, null, null);
        }
        var pos = 0;
        var msg = MetadataParser.ReadFixLenString(302, body, ref pos);
        var code = body[pos++];
        var isLast = body[pos++];
        if(pos != body.Length)
            throw new DbnSerializationError("Did not consume all data");
        return new ErrorMessage(in header, msg, code, isLast > 0);
    }

    internal static SymbolMappingMsg ParseSymbolMappingMsg(Metadata metadata, in RecordHeader header, ReadOnlySpan<byte> body)
    {
        var pos = 0;
        var sTypeIn = metadata.Version > 1 ? (SymbolType) body[pos++] : SymbolType.RawSymbol;
        var symbolIn = MetadataParser.ReadFixLenStringPooled(metadata.SymbolCStrLen, body, ref pos);
        var sTypeOut = metadata.Version > 1 ? (SymbolType) body[pos++] : SymbolType.RawSymbol;
        var symbolOut = MetadataParser.ReadFixLenStringPooled(metadata.SymbolCStrLen, body, ref pos);
        return new SymbolMappingMsg(in header, sTypeIn, symbolIn, sTypeOut, symbolOut);
    }

    public static string ParseAscii(ReadOnlySpan<byte> span)
    {
        var nullIndex = span.IndexOf((byte) 0);
        if (nullIndex >= 0)
            span = span.Slice(0, nullIndex);
        return Encoding.ASCII.GetString(span);
    }
    public static string ParseAsciiPooled(ReadOnlySpan<byte> span)
    {
        var nullIndex = span.IndexOf((byte) 0);
        if (nullIndex >= 0)
            span = span.Slice(0, nullIndex);
        return StringPool.Shared.GetOrAdd(span, Encoding.ASCII);
    }
}