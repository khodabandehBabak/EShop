using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

public interface IBrandService
{
    Task<List<BrandListItemDto>> GetAllAsync(string? search = null, bool? isActive = null);
    Task<BrandEditDto?> GetForEditAsync(int id);
    Task<int> CreateAsync(BrandEditDto dto);
    Task UpdateAsync(BrandEditDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
}