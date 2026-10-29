using Microsoft.AspNetCore.Authorization;

namespace EShop.Infrastructure.Authorization;

/// <summary>
/// Attribute برای اعمال Permission-based Authorization
/// استفاده: [HasPermission("products.view")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "PERM_";

    public HasPermissionAttribute(string permission)
        : base($"{PolicyPrefix}{permission}")
    {
    }
}