using Microsoft.AspNetCore.Authorization;

namespace EShop.Infrastructure.Authorization;

/// <summary>
/// Requirement که مشخص می‌کند یک Permission خاص لازم است
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}