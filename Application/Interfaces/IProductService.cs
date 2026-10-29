using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس مدیریت محصولات (پنل ادمین)
/// </summary>
public interface IProductService
{
    Task<(List<ProductListItemDto> Items, int Total)> GetAllAsync(
        string? search = null, int? categoryId = null, int? brandId = null,
        bool? isActive = null, int page = 1, int pageSize = 20);

    Task<ProductEditDto?> GetForEditAsync(int id);
    Task<int> CreateAsync(ProductEditDto dto);
    Task UpdateAsync(ProductEditDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> ToggleFeaturedAsync(int id);

    /// <summary>دریافت ویژگی‌ها برای پر کردن Dropdownها</summary>
    Task<List<AttributeWithOptionsDto>> GetAttributesAsync();
}