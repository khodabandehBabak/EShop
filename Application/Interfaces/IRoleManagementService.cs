using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

public interface IRoleManagementService
{
    Task<List<RoleListItemDto>> GetAllAsync();
    Task<RoleEditDto?> GetForEditAsync(int id);
    Task<int> CreateAsync(RoleCreateDto dto);
    Task UpdateAsync(RoleEditDto dto);
    Task<bool> DeleteAsync(int id);
}