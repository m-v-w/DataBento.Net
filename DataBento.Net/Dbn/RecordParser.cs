using System.Buffers.Binary;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace DataBento.Net.Dbn;

public static class RecordParser
{
    internal static SystemMessage ParseSystemMsg(Metadata metadata, in RecordHeader header, ReadOnlySpan<byte> body)
    {
        if (metadata.Version == 1)
        {
            var v1Pos = 0;
            var v1Msg = MetadataParser.ReadFixLenString(64, body, ref v1Pos);
            if(v1Pos != body.Length)
                throw new InvalidOperationException("Did not consume all data");
            return new SystemMessage(in header, v1Msg, null);
        }
        var pos = 0;
        var msg = MetadataParser.ReadFixLenString(303, body, ref pos);
        var code = body[pos++];
        if(pos != body.Length)
            throw new InvalidOperationException("Did not consume all data");
        return new SystemMessage(in header, msg, code);
    }
    internal static ErrorMessage ParseErrorMsg(Metadata metadata, in RecordHeader header, ReadOnlySpan<byte> body)
    {
        if (metadata.Version == 1)
        {
            var v1Pos = 0;
            var v1Msg = MetadataParser.ReadFixLenString(64, body, ref v1Pos);
            if(v1Pos != body.Length)
                throw new InvalidOperationException("Did not consume all data");
            return new ErrorMessage(in header, v1Msg, null, null);
        }
        var pos = 0;
        var msg = MetadataParser.ReadFixLenString(302, body, ref pos);
        var code = body[pos++];
        var isLast = body[pos++];
        if(pos != body.Length)
            throw new InvalidOperationException("Did not consume all data");
        return new ErrorMessage(in header, msg, code, isLast > 0);
    }

    internal static SymbolMappingMsg ParseSymbolMappingMsg(Metadata metadata, in RecordHeader header, ReadOnlySpan<byte> body)
    {
        var pos = 0;
        var sTypeIn = metadata.Version > 1 ? (SymbolType) body[pos++] : SymbolType.InstrumentId;
        var symbolIn = MetadataParser.ReadFixLenStringPooled(metadata.SymbolCStrLen, body, ref pos);
        var sTypeOut = metadata.Version > 1 ? (SymbolType) body[pos++] : SymbolType.InstrumentId;
        var symbolOut = MetadataParser.ReadFixLenStringPooled(metadata.SymbolCStrLen, body, ref pos);
        return new SymbolMappingMsg(in header, sTypeIn, symbolIn, sTypeOut, symbolOut);
    }
}