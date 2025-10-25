using System.ComponentModel;
using System.Reflection;

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
            dict[value] = attr?.Description ?? str;
        }
        return dict;
    }
}