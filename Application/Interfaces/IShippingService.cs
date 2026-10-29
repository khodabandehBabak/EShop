using EShop.Application.DTOs;
using EShop.Domain.Enums;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس حمل‌ونقل — هزینه‌ها از SiteSettings خوانده می‌شوند
/// </summary>
public interface IShippingService
{
    Task<List<ShippingOptionDto>> GetOptionsAsync(decimal subTotal, string city);
    Task<decimal> GetCostAsync(ShippingMethod method, decimal subTotal);
}