namespace Library.Common;

public static class EnumExtensions
{
    public static string ToApiString<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        return value.ToString().ToUpperInvariant();
    }
}
