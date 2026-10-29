using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس محصولات در Storefront (نمایش عمومی)
/// </summary>
public interface IProductStorefrontService
{
    Task<ProductDetailDto?> GetBySlugAsync(string slug);
    Task<List<ProductCardDto>> GetRelatedAsync(int productId, int categoryId, int take = 8);
    Task IncrementViewAsync(int productId);
}