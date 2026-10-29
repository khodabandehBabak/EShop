using EShop.Application.DTOs;
using EShop.Domain.Enums;

namespace EShop.Application.Interfaces;

public interface ISliderService
{
    Task<List<SliderListItemDto>> GetAllAsync(SliderPosition? position = null);
    Task<SliderEditDto?> GetForEditAsync(int id);
    Task<int> CreateAsync(SliderEditDto dto);
    Task UpdateAsync(SliderEditDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleActiveAsync(int id);

    /// <summary>دریافت اسلایدرهای فعال (برای Storefront)</summary>
    Task<List<SliderPublicDto>> GetActiveAsync(SliderPosition position, int take = 5);
}