using System.Security.Claims;

namespace AuthApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal user)
    {
        var id =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(id))
            throw new Exception("No user id claim found in token");

        return Guid.Parse(id);
    }
}
