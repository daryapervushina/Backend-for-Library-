using Library.Common.Exceptions;

namespace Library.Common;

public static class EnumParsing
{
    /// <summary>Parses an API string (case-insensitive name) or throws 400.</summary>
    public static TEnum Parse<TEnum>(string value, string field) where TEnum : struct, Enum
    {
        var trimmed = value.Trim();

        if (trimmed.Length == 0
            || !trimmed.All(char.IsLetter)
            || !Enum.TryParse<TEnum>(trimmed, true, out var result))
        {
            throw new BadRequestException($"{field} has invalid value");
        }

        return result;
    }
}
