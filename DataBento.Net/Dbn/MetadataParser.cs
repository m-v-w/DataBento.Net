using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace DataBento.Net.Dbn;

public static class MetadataSerializer
{
    public static Metadata Deserialize(ReadOnlySpan<byte> data, byte version)
    {
        if(version != 2 && version != 3)
            throw new DbnSerializationError($"Unsupported metadata version: {version}");
        var pos = 0;
        var dataset = ReadFixLenString(16, data, ref pos);
        var schema = (SchemaId) BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pos,2));
        pos += 2;
        var start = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(pos,8));
        pos += 8;
        var end = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(pos,8));
        pos += 8;
        var limit = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(pos,8));
        pos += 8;
        var sTypeIn = (SymbolType) data[pos++];
        var sTypeOut = (SymbolType) data[pos++];
        var tsOut = data[pos++];
        var symbolCStrLen = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pos, 2));
        pos += 2;
        var schemaDefLen = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pos, 4));
        pos += 4 + checked((int) schemaDefLen);
        var symbols = ReadFixLenStringArray(symbolCStrLen, data, ref pos);
        var partialSymbols = ReadFixLenStringArray(symbolCStrLen, data, ref pos);
        var notFound = ReadFixLenStringArray(symbolCStrLen, data, ref pos);
        var mappings = ReadSymbolMappings(symbolCStrLen, data, ref pos);
        if(pos != data.Length)
            throw new DbnSerializationError("Did not consume all data");
        return new Metadata(version, dataset, schema, checked((long) start), checked((long) end), limit, 
            sTypeIn, sTypeOut, tsOut > 0, symbols, partialSymbols, notFound, mappings);
    }
    private static SymbolMapping[] ReadSymbolMappings(int symbolCStrLen, ReadOnlySpan<byte> data, ref int pos)
    {
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pos, 4));
        pos += 4;
        var result = new SymbolMapping[checked((int) count)];
        for (var i = 0; i < result.Length; i++)
        {
            var rawSymbol = ReadFixLenString(symbolCStrLen, data, ref pos);
            var intervalCount = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pos, 4));
            pos += 4;
            var intervals = new MappingInterval[checked((int) intervalCount)];
            for(var j = 0; j < intervalCount; j++)
            {
                var startDate = ReadDateInt(data, ref pos);
                var endDate = ReadDateInt(data, ref pos);
                var symbol = ReadFixLenString(symbolCStrLen, data, ref pos);
                intervals[j] = new MappingInterval(startDate, endDate, symbol);
            }
            result[i] = new SymbolMapping(rawSymbol, intervals);
        }
        return result;
    }
    private static DateOnly ReadDateInt(ReadOnlySpan<byte> data, ref int pos)
    {
        var intVal = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pos, 4));
        pos += 4;
        var day = (int)(intVal % 100);
        intVal /= 100;
        var month = (int)(intVal % 100);
        intVal /= 100;
        var year = checked((int) intVal);
        if(year < 1900 || year > 2100)
            throw new DbnSerializationError($"Invalid year value: {year}");
        return new DateOnly(year, month, day);
    }
    private static string[] ReadFixLenStringArray(int strLen, ReadOnlySpan<byte> data, ref int pos)
    {
        var count = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pos, 4));
        pos += 4;
        var result = new string[checked((int) count)];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = StringPool.Shared.GetOrAdd(data.Slice(pos, strLen), Encoding.UTF8);
            pos += strLen;
        }
        return result;
    }
    private static string ReadFixLenString(int strLen, ReadOnlySpan<byte> data, ref int pos)
    {
        var result = StringPool.Shared.GetOrAdd(data.Slice(pos, strLen), Encoding.UTF8);
        pos += strLen;
        return result;
    }
}