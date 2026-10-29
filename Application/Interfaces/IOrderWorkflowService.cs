using EShop.Application.DTOs;
using EShop.Domain.Enums;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس مدیریت چرخه سفارش (پردازش، بسته‌بندی، ارسال، تحویل، لغو)
/// </summary>
public interface IOrderWorkflowService
{
    // Admin
    Task<(List<OrderAdminListItemDto> Items, int Total)> GetForAdminAsync(OrderAdminFilterDto filter);
    Task<OrderAdminDetailDto?> GetAdminDetailAsync(int orderId);
    Task<int> GetPendingCountAsync();

    // Workflow transitions
    Task<(bool Ok, string Message)> StartProcessingAsync(int orderId, int adminId);
    Task<(bool Ok, string Message)> MarkAsPackedAsync(int orderId, int adminId, string? note = null);
    Task<(bool Ok, string Message)> MarkAsShippedAsync(ShipOrderDto dto, int adminId);
    Task<(bool Ok, string Message)> MarkAsDeliveredAsync(int orderId, int adminId);
    Task<(bool Ok, string Message)> CancelAsync(CancelOrderDto dto, int adminId);

    // Bulk actions
    Task<(int Success, int Failed)> BulkPackAsync(IEnumerable<int> orderIds, int adminId);
    Task<(int Success, int Failed)> BulkShipAsync(
        IEnumerable<int> orderIds, string company, int adminId);

    // Customer
    Task<OrderTrackingDto?> GetTrackingAsync(int orderId, int userId);
    Task<List<OrderTrackingDto>> GetCustomerOrdersAsync(int userId, OrderStatus? status = null);
}