using EShop.Application.DTOs;
using EShop.Domain.Enums;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس سبد خرید (Session-based)
/// </summary>
public interface ICartService
{
    /// <summary>افزودن محصول بدون variant</summary>
    Task AddAsync(int productId, int quantity);

    /// <summary>افزودن محصول با یا بدون variant</summary>
    Task AddAsync(int productId, int? variantId, int quantity);

    /// <summary>بروزرسانی تعداد</summary>
    Task UpdateQuantityAsync(int productId, int? variantId, int quantity);

    /// <summary>حذف از سبد</summary>
    Task RemoveAsync(int productId, int? variantId);

    /// <summary>خلاصه‌ی سبد با هزینه ارسال</summary>
    Task<CheckoutSummaryDto> GetSummaryAsync(ShippingMethod? method = null);

    /// <summary>تعداد کل اقلام</summary>
    Task<int> GetItemCountAsync();

    /// <summary>خالی کردن سبد</summary>
    void Clear();
}