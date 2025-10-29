using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.HighPerformance.Buffers;

namespace DataBento.Net.Tcp.ControlMessages;

internal static class EnumSerializer<T> where T : Enum
{
    private static readonly Dictionary<T, string> Values = CreateStrings();
    public static string ToString(T value)
    {
        return Values[value];
    }
    private static Dictionary<T, string> CreateStrings()
    {
        var dict = new Dictionary<T, string>();
        foreach (T value in Enum.GetValues(typeof(T)))
        {
            var str = value.ToString();
            var fieldInfo = typeof(T).GetField(str) ?? throw new InvalidProgramException("invalid enum value");
            var attr = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
            dict[value] = StringPool.Shared.GetOrAdd(attr?.Description ?? str.ToLower());
        }
        return dict;
    }

    public static T FromString(string value, StringComparison comparison = StringComparison.InvariantCulture)
    {
        return Values.Single(x => string.Equals(x.Value, value, comparison)).Key;
    }
}
internal class JsonEnumSerializer<T> : JsonConverter<T> where T : Enum
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ReadString(ref reader),
            JsonTokenType.Number => ReadInt(ref reader),
            JsonTokenType.Null => default,
            _ => throw new JsonException($"Expected token, got {reader.TokenType}"),
        };
    }

    private T ReadInt(ref Utf8JsonReader reader)
    {
        var intValue = reader.GetInt32();
        return (T) Enum.ToObject(typeof(T), intValue);
    }
    private T ReadString(ref Utf8JsonReader reader)
    {
        var str = reader.GetString();
        if (str == null)
            throw new JsonException("Expected non-null string");
        return EnumSerializer<T>.FromString(str);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var str = EnumSerializer<T>.ToString(value);
        writer.WriteStringValue(str);
    }
}