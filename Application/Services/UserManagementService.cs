using EShop.Application.Data;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public UserManagementService(IApplicationDbContext db, UserManager<AppUser> userManager)
    {
        _db = db; _userManager = userManager;
    }

    public async Task<(List<UserListItemDto> Items, int Total)> GetAllAsync(
        string? search, string? roleName, bool? isActive, int page, int pageSize)
    {
        var q = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(u => u.FirstName.Contains(search) ||
                             u.LastName.Contains(search) ||
                             (u.PhoneNumber != null && u.PhoneNumber.Contains(search)) ||
                             (u.Email != null && u.Email.Contains(search)));

        if (isActive.HasValue)
            q = q.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            var roleId = await _db.Roles
                .Where(r => r.Name == roleName).Select(r => r.Id).FirstOrDefaultAsync();
            if (roleId > 0)
                q = q.Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
        }

        var total = await q.CountAsync();

        var users = await q
            .OrderByDescending(u => u.RegisteredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.UserName,
                u.Email,
                u.PhoneNumber,
                u.IsActive,
                u.PhoneNumberConfirmed,
                u.RegisteredAt,
                u.LastLoginAt
            })
            .ToListAsync();

        var ids = users.Select(u => u.Id).ToList();

        var roleMap = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, Role = r.Name! }
        ).ToListAsync();

        var result = users.Select(u => new UserListItemDto
        {
            Id = u.Id,
            FullName = $"{u.FirstName} {u.LastName}".Trim(),
            UserName = u.UserName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            PhoneNumberConfirmed = u.PhoneNumberConfirmed,
            RegisteredAt = u.RegisteredAt,
            LastLoginAt = u.LastLoginAt,
            Roles = roleMap.Where(r => r.UserId == u.Id).Select(r => r.Role).ToList()
        }).ToList();

        return (result, total);
    }

    public async Task<UserEditDto?> GetForEditAsync(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        return new UserEditDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber ?? "",
            Email = user.Email,
            NationalCode = user.NationalCode,
            IsActive = user.IsActive,
            UserNameDisplay = user.PhoneNumber ?? user.UserName ?? "",
            RegisteredAt = user.RegisteredAt,
            RoleIds = await _db.UserRoles
                .Where(ur => ur.UserId == id).Select(ur => ur.RoleId).ToListAsync(),
            DirectPermissionIds = await _db.UserPermissions
                .Where(up => up.UserId == id && up.IsGranted)
                .Select(up => up.PermissionId).ToListAsync()
        };
    }

    public async Task<int> CreateAsync(UserCreateDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
            throw new InvalidOperationException("این شماره موبایل قبلاً ثبت شده است");

        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("این ایمیل قبلاً ثبت شده است");

        var user = new AppUser
        {
            UserName = dto.PhoneNumber,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            NationalCode = dto.NationalCode,
            IsActive = dto.IsActive,
            PhoneNumberConfirmed = true,
            RegisteredAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("، ",
                result.Errors.Select(e => e.Description)));

        await AssignRolesAsync(user.Id, dto.RoleIds);
        await AssignDirectPermissionsAsync(user.Id, dto.DirectPermissionIds);

        return user.Id;
    }

    public async Task UpdateAsync(UserEditDto dto, int currentUserId)
    {
        var user = await _db.Users.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("کاربر یافت نشد");

        var isSelf = user.Id == currentUserId;
        if (isSelf && !dto.IsActive)
            throw new InvalidOperationException("نمی‌توانید حساب خود را غیرفعال کنید");

        if (user.PhoneNumber != dto.PhoneNumber &&
            await _db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != dto.Id))
            throw new InvalidOperationException("این شماره موبایل قبلاً ثبت شده است");

        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != dto.Id))
            throw new InvalidOperationException("این ایمیل قبلاً ثبت شده است");

        var wasActive = user.IsActive;

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        user.Email = dto.Email;
        user.NationalCode = dto.NationalCode;
        user.IsActive = dto.IsActive;

        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!reset.Succeeded)
                throw new InvalidOperationException(string.Join("، ",
                    reset.Errors.Select(e => e.Description)));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await AssignRolesAsync(user.Id, dto.RoleIds);

        var currentPerms = await _db.UserPermissions
            .Where(up => up.UserId == user.Id).ToListAsync();
        _db.UserPermissions.RemoveRange(currentPerms);
        await _db.SaveChangesAsync();
        await AssignDirectPermissionsAsync(user.Id, dto.DirectPermissionIds);

        if (wasActive && !user.IsActive)
            await _userManager.UpdateSecurityStampAsync(user);
    }

    public async Task<bool> DeleteAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            throw new InvalidOperationException("نمی‌توانید حساب خود را حذف کنید");

        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        if (await _db.Orders.AnyAsync(o => o.CustomerId == id))
        {
            user.IsActive = false;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return true;
        }

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ToggleActiveAsync(int id, int currentUserId)
    {
        if (id == currentUserId)
            throw new InvalidOperationException("نمی‌توانید حساب خود را غیرفعال کنید");

        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        if (!user.IsActive)
            await _userManager.UpdateSecurityStampAsync(user);

        return user.IsActive;
    }

    private async Task AssignRolesAsync(int userId, List<int> roleIds)
    {
        if (roleIds == null || !roleIds.Any()) return;

        var roleNames = await _db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync();

        if (roleNames.Any())
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            await _userManager.AddToRolesAsync(user!, roleNames);
        }
    }

    private async Task AssignDirectPermissionsAsync(int userId, List<int> permissionIds)
    {
        if (permissionIds == null || !permissionIds.Any()) return;

        var validIds = await _db.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id).ToListAsync();

        foreach (var pid in validIds)
        {
            _db.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = pid,
                IsGranted = true
            });
        }
        await _db.SaveChangesAsync();
    }
}