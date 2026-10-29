using EShop.Application.DTOs;
using EShop.Domain.Entities;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس اصلی سفارش (ایجاد، پرداخت)
/// </summary>
public interface IOrderService
{
    Task<Order> CreateAsync(CheckoutInputDto input, CheckoutSummaryDto summary);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<Order?> GetByIdAsync(int id);
    Task MarkAsPaidAsync(int orderId, string refId);
    Task MarkAsFailedAsync(int orderId, string? reason = null);
}