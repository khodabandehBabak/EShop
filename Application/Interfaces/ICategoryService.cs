using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryListItemDto>> GetAllAsync(string? search = null, bool? isActive = null);
    Task<List<CategoryTreeItemDto>> GetTreeAsync(bool onlyActive = false);
    Task<CategoryEditDto?> GetForEditAsync(int id);
    Task<int> CreateAsync(CategoryEditDto dto);
    Task UpdateAsync(CategoryEditDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task<List<MegaMenuCategoryDto>> GetMenuAsync();
}