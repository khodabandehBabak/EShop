using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EShop.Infrastructure.Authorization;

/// <summary>
/// Policy Provider سفارشی که به‌صورت داینامیک Policyها را برای
/// [HasPermission("...")] ایجاد می‌کند
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName.Substring(
                HasPermissionAttribute.PolicyPrefix.Length);

            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}