using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EShop.Domain.Entities;
using EShop.Infrastructure.Data;

namespace EShop.Infrastructure.Identity;

/// <summary>
/// هنگام لاگین، Claims کامل (اطلاعات کاربر + Permissionها) را
/// به Identity اضافه می‌کند
/// </summary>
public class AppUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<AppUser, AppRole>
{
    private readonly AppDbContext _db;

    public AppUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        AppDbContext db)
        : base(userManager, roleManager, optionsAccessor)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // اطلاعات شخصی
        identity.AddClaim(new Claim("FirstName", user.FirstName ?? ""));
        identity.AddClaim(new Claim("LastName", user.LastName ?? ""));
        identity.AddClaim(new Claim("FullName",
            $"{user.FirstName} {user.LastName}".Trim()));

        if (!string.IsNullOrEmpty(user.PhoneNumber))
            identity.AddClaim(new Claim("PhoneNumber", user.PhoneNumber));

        // ============ جمع‌آوری مجوزها از نقش‌ها ============
        var rolePerms = await _db.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(_db.RolePermissions,
                ur => ur.RoleId, rp => rp.RoleId,
                (ur, rp) => rp.PermissionId)
            .Join(_db.Permissions,
                pid => pid, p => p.Id,
                (pid, p) => p.Code)
            .Distinct()
            .ToListAsync();

        // ============ مجوزهای مستقیم کاربر (Override) ============
        var userPerms = await _db.UserPermissions
            .Where(up => up.UserId == user.Id)
            .Join(_db.Permissions,
                up => up.PermissionId, p => p.Id,
                (up, p) => new { p.Code, up.IsGranted })
            .ToListAsync();

        // ترکیب: نقش‌ها + grants - denies
        var finalSet = new HashSet<string>(rolePerms);
        foreach (var up in userPerms)
        {
            if (up.IsGranted) finalSet.Add(up.Code);
            else finalSet.Remove(up.Code); // deny بر allow اولویت دارد
        }

        foreach (var code in finalSet)
            identity.AddClaim(new Claim("permission", code));

        return identity;
    }
}