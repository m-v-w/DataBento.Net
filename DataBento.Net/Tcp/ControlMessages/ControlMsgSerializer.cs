using System.Buffers;
using System.Reflection;
using System.Text;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

namespace DataBento.Net.Tcp.ControlMessages;

public static class ControlMsgSerializer
{
    internal const byte NewLine = (byte) '\n';
    internal const byte Pipe = (byte) '|';
    internal const byte Equal = (byte) '=';
    internal const byte One = (byte) '1';
    internal const byte Zero = (byte) '0';
    internal const byte Comma = (byte)',';

    private static readonly Dictionary<string, Type> PrefixTypeMap = CreatePrefixTypeMap();
    private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> TypePropertyInfoCache 
        = PrefixTypeMap.Values.ToDictionary(x=> x, CreatePropertyInfoMap);
    public static void Serialize(IControlMsg msg, IBufferWriter<byte> writer)
    {
        if (msg is IControlMsgWithSerializer controlMsgWithSerializer)
        {
            controlMsgWithSerializer.Serialize(writer);
            return;
        }
        var propertyInfos = GetPropertyInfoMap(msg.GetType());
        var first = true;
        foreach (var (attr, propertyInfo) in propertyInfos)
        {
            var value = propertyInfo.GetValue(msg);
            if (value == null) 
                continue;
            if(!first)
                writer.Write(Pipe);
            writer.WriteAscii(attr);
            writer.Write(Equal);
            writer.WriteValue(value, propertyInfo.PropertyType);
            first = false;
        }
        writer.Write(NewLine);
    }

    internal static void WriteAscii(this IBufferWriter<byte> writer, ReadOnlySpan<char> value)
    {
        var span = writer.GetSpan(value.Length);
        if(!Encoding.ASCII.TryGetBytes(value, span, out int bytesWritten))
            throw new InvalidOperationException("Failed to encode ascii");
        writer.Advance(bytesWritten);
    }
    private static void WriteValue(this IBufferWriter<byte> writer, object value, Type propertyType)
    {
        if (value is bool boolValue)
        {
            writer.Write(boolValue ? One : Zero);
            return;
        }
        if (value is string stringValue)
        {
            WriteAscii(writer, stringValue);
            return;
        }
        if (propertyType.IsEnum)
        {
            WriteAscii(writer,value.ToString()!.ToLowerInvariant());
            return;
        }
        if (propertyType.IsGenericType)
        {
            var nullableType = Nullable.GetUnderlyingType(propertyType);
            if (nullableType != null)
            {
                WriteValue(writer, value, nullableType);
                return;
            }
        }
        WriteAscii(writer,value.ToString() ?? string.Empty);
    }
    private static Dictionary<string, PropertyInfo> GetPropertyInfoMap(Type type)
    {
        if(TypePropertyInfoCache.TryGetValue(type, out var result))
            return result;
        return CreatePropertyInfoMap(type);
    }
    
    public static IControlMsg Deserialize(ReadOnlySpan<char> msg)
    {
        if(msg[^1] == '\n')
            msg = msg.Slice(0, msg.Length - 1);
        if(msg.Length == 0)
            throw new ControlMsgSerializationError("Empty message");
        var eqIdx = msg.IndexOf('=');
        if(eqIdx < 0)
            throw new ControlMsgSerializationError($"Invalid message: {msg}");
        var prefix = StringPool.Shared.GetOrAdd(msg.Slice(0, eqIdx));
        if(!PrefixTypeMap.TryGetValue(prefix, out var msgType))
            throw new ControlMsgSerializationError($"Unknown message prefix: {prefix}");
        var propertyInfos = GetPropertyInfoMap(msgType);
        var result = Activator.CreateInstance(msgType)!;
        var fieldEnumerator = msg.Split('|');
        while (fieldEnumerator.MoveNext())
        {
            var field = msg[fieldEnumerator.Current];
            var equalIndex = field.IndexOf('=');
            if (equalIndex < 0)
                throw new ControlMsgSerializationError($"Invalid field: {msg}");
            var fieldName = StringPool.Shared.GetOrAdd(field.Slice(0, equalIndex));
            if(!propertyInfos.TryGetValue(fieldName, out var propertyInfo))
                throw new ControlMsgSerializationError($"Unknown field: {field} in {msgType}");
            var fieldValue = field.Slice(equalIndex + 1);
            propertyInfo.SetValue(result, DeserializeValue(fieldValue, propertyInfo.PropertyType));
        }
        return (IControlMsg) result;
    }
    private static object? DeserializeValue(ReadOnlySpan<char> fieldValue, Type propertyType)
    {
        if(fieldValue.Length == 0)
            throw new ControlMsgSerializationError($"Empty value for type {propertyType}");
        if(propertyType == typeof(bool))
        {
            if(!int.TryParse(fieldValue, out var intValue))
                throw new ControlMsgSerializationError($"Invalid bool value: {fieldValue}");
            return intValue != 0;
        }
        if(propertyType.IsEnum)
        {
            try
            {
                return Enum.Parse(propertyType, fieldValue, true);
            }
            catch (ArgumentException)
            {
                throw new ControlMsgSerializationError($"Invalid enum value: {fieldValue} for enum {propertyType}");
            }
        }
        return Convert.ChangeType(new string(fieldValue), propertyType);
    }
    
    private static Dictionary<string, Type> CreatePrefixTypeMap()
    {
        var assembly = Assembly.GetAssembly(typeof(GreetingMsg)) ?? throw new InvalidProgramException("Failed to get assembly of GreetingsMsg");
        return assembly.GetTypes()
            .Where(x => x.GetCustomAttribute<ControlMsgResponseAttribute>() != null)
            .ToDictionary(x => x.GetCustomAttribute<ControlMsgResponseAttribute>()!.Prefix);
    }
    private static Dictionary<string, PropertyInfo> CreatePropertyInfoMap(Type type)
    {
        return type.GetProperties()
            .Where(x => x.GetCustomAttribute<ControlMsgFieldAttribute>() != null)
            .ToDictionary(x => x.GetCustomAttribute<ControlMsgFieldAttribute>()!.Name);
    }
}
public class ControlMsgSerializationError : Exception
{
    public ControlMsgSerializationError(string message) : base(message)
    {
    }
}