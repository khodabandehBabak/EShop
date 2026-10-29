using System.Security.Claims;

namespace EShop.Application.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool HasPermission(this ClaimsPrincipal user, string permission)
    {
        if (user.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole("SuperAdmin")) return true;
        return user.HasClaim("permission", permission);
    }

    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
        => permissions.Any(user.HasPermission);

    public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
        => permissions.All(user.HasPermission);
}