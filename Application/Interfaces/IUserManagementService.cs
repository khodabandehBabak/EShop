using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

public interface IUserManagementService
{
    Task<(List<UserListItemDto> Items, int Total)> GetAllAsync(
        string? search = null, string? roleName = null, bool? isActive = null,
        int page = 1, int pageSize = 20);

    Task<UserEditDto?> GetForEditAsync(int id);
    Task<int> CreateAsync(UserCreateDto dto);
    Task UpdateAsync(UserEditDto dto, int currentUserId);
    Task<bool> DeleteAsync(int id, int currentUserId);
    Task<bool> ToggleActiveAsync(int id, int currentUserId);
}