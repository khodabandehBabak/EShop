using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Application.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

namespace EShop.Application.Services;

public class RoleManagementService : IRoleManagementService
{
    private static readonly HashSet<string> ProtectedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SuperAdmin", "Admin", "Customer"
    };

    private readonly IApplicationDbContext _db;
    private readonly RoleManager<AppRole> _roleManager;

    public RoleManagementService(IApplicationDbContext db, RoleManager<AppRole> roleManager)
    {
        _db = db; _roleManager = roleManager;
    }

    public async Task<List<RoleListItemDto>> GetAllAsync()
    {
        var roles = await _db.Roles
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description,
                UserCount = _db.UserRoles.Count(ur => ur.RoleId == r.Id),
                PermissionCount = _db.RolePermissions.Count(rp => rp.RoleId == r.Id)
            })
            .OrderBy(r => r.Name)
            .ToListAsync();

        foreach (var r in roles)
            r.IsSystemRole = ProtectedRoles.Contains(r.Name);

        return roles;
    }

    public async Task<RoleEditDto?> GetForEditAsync(int id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return null;

        return new RoleEditDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            PermissionIds = await _db.RolePermissions
                .Where(rp => rp.RoleId == id).Select(rp => rp.PermissionId).ToListAsync(),
            IsSystemRole = ProtectedRoles.Contains(role.Name!),
            UserCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == id)
        };
    }

    public async Task<int> CreateAsync(RoleCreateDto dto)
    {
        if (await _roleManager.RoleExistsAsync(dto.Name))
            throw new InvalidOperationException("نقشی با این نام قبلاً وجود دارد");

        var role = new AppRole(dto.Name) { Description = dto.Description };
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("، ",
                result.Errors.Select(e => e.Description)));

        await SavePermissionsAsync(role.Id, dto.PermissionIds);
        return role.Id;
    }

    public async Task UpdateAsync(RoleEditDto dto)
    {
        var role = await _db.Roles.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("نقش یافت نشد");

        if (ProtectedRoles.Contains(role.Name!) && role.Name != dto.Name)
            throw new InvalidOperationException("نام نقش‌های سیستمی قابل تغییر نیست");

        if (role.Name != dto.Name)
        {
            var existing = await _roleManager.FindByNameAsync(dto.Name);
            if (existing != null && existing.Id != dto.Id)
                throw new InvalidOperationException("نام نقش تکراری است");

            role.Name = dto.Name;
            role.NormalizedName = dto.Name.ToUpperInvariant();
        }

        role.Description = dto.Description;

        if (!ProtectedRoles.Contains(role.Name!))
        {
            var current = await _db.RolePermissions
                .Where(rp => rp.RoleId == role.Id).ToListAsync();
            _db.RolePermissions.RemoveRange(current);
            await _db.SaveChangesAsync();
        }

        await SavePermissionsAsync(role.Id, dto.PermissionIds);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return false;

        if (ProtectedRoles.Contains(role.Name!))
            throw new InvalidOperationException("نقش سیستمی قابل حذف نیست");

        if (await _db.UserRoles.AnyAsync(ur => ur.RoleId == id))
            throw new InvalidOperationException("این نقش به کاربرانی اختصاص یافته و قابل حذف نیست");

        var result = await _roleManager.DeleteAsync(role);
        return result.Succeeded;
    }

    private async Task SavePermissionsAsync(int roleId, List<int> permissionIds)
    {
        if (permissionIds == null || !permissionIds.Any()) return;

        var validIds = await _db.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Id).ToListAsync();

        foreach (var pid in validIds)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = pid
            });
        }
        await _db.SaveChangesAsync();
    }
}