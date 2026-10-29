using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Security;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace EShop.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IApplicationDbContext _db;
    public PermissionService(IApplicationDbContext db) => _db = db;

    public async Task<List<PermissionGroupDto>> GetAllGroupedAsync()
    {
        var perms = await _db.Permissions
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PermissionItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Group = p.Group,
                DisplayOrder = p.DisplayOrder
            })
            .ToListAsync();

        return perms
            .GroupBy(p => p.Group)
            .Select(g => new PermissionGroupDto
            {
                Group = g.Key,
                Permissions = g.OrderBy(p => p.DisplayOrder).ToList()
            })
            .ToList();
    }

    public async Task<List<PermissionItemDto>> GetAllAsync()
    {
        return await _db.Permissions
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new PermissionItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Group = p.Group,
                DisplayOrder = p.DisplayOrder
            })
            .ToListAsync();
    }

    public async Task SeedAsync()
    {
        var existingCodes = await _db.Permissions.Select(p => p.Code).ToListAsync();
        var existingSet = existingCodes.ToHashSet();

        foreach (var (code, name, group, order) in PermissionCatalog.All)
        {
            if (existingSet.Contains(code)) continue;
            _db.Permissions.Add(new Permission
            {
                Code = code,
                Name = name,
                Group = group,
                DisplayOrder = order
            });
        }
        await _db.SaveChangesAsync();
    }
}