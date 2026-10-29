using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس ویژگی‌های محصول (CRUD + Optionها)
/// </summary>
public interface IProductAttributeService
{
    // Attributes
    Task<List<AttributeListItemDto>> GetAllAsync(string? search = null);
    Task<AttributeEditDto?> GetForEditAsync(int id);
    Task<int> CreateAsync(AttributeEditDto dto);
    Task UpdateAsync(AttributeEditDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleFilterableAsync(int id);

    // Options
    Task<OptionItemDto> AddOptionAsync(int attributeId, OptionCreateDto dto);
    Task<OptionItemDto> UpdateOptionAsync(int attributeId, OptionUpdateDto dto);
    Task<bool> DeleteOptionAsync(int attributeId, int optionId);
    Task<bool> ReorderOptionsAsync(int attributeId, List<int> orderedOptionIds);
}