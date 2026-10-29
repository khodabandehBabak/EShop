using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس صفحه فروشگاه (فیلتر، مرتب‌سازی، Facets)
/// </summary>
public interface IShopService
{
    Task<ShopResultDto> GetProductsAsync(ShopFilterDto filter);
    Task<ShopResultDto> GetProductsForCategoryAsync(string categorySlug, ShopFilterDto filter);
    Task<ShopResultDto> GetProductsForBrandAsync(string brandSlug, ShopFilterDto filter);
    Task<ShopResultDto> SearchAsync(string query, ShopFilterDto filter);

    /// <summary>ساخت URL با تغییر یک filter (برای لینک‌های sidebar)</summary>
    string BuildUrl(ShopFilterDto current, Action<ShopFilterDto> mutate);
}