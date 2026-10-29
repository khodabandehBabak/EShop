using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

public interface IPermissionService
{
    Task<List<PermissionGroupDto>> GetAllGroupedAsync();
    Task<List<PermissionItemDto>> GetAllAsync();

    /// <summary>Seed مجوزها از PermissionCatalog در دیتابیس</summary>
    Task SeedAsync();
}