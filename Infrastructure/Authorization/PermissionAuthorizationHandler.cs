using Microsoft.AspNetCore.Authorization;

namespace EShop.Infrastructure.Authorization;

/// <summary>
/// بررسی می‌کند آیا کاربر جاری Claim با کد Permission دارد یا خیر
/// SuperAdmin به‌صورت خودکار همه‌ی دسترسی‌ها را دارد
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // SuperAdmin همه‌کاره است
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // بررسی Claim مجوز
        if (context.User.HasClaim("permission", requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}