using Library.Models.Enums;

namespace Library.Common;

public static class UserRoleExtensions
{
    public static string ToApiString(this UserRole role) => role.ToString().ToUpperInvariant();
}
