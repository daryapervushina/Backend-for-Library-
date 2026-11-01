using System.Security.Claims;
using Library.Common.Exceptions;
using Library.Models.Enums;

namespace Library.Common;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst("sub")?.Value;

        return int.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("Invalid token");
    }

    public static bool IsLibrarian(this ClaimsPrincipal user)
    {
        return user.FindFirst("role")?.Value == UserRole.Librarian.ToApiString();
    }
}
