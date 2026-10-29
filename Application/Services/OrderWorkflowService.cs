using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using EShop.Application.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace EShop.Application.Services;

public class OrderWorkflowService : IOrderWorkflowService
{
    private readonly IApplicationDbContext _db;
    private readonly IHubContext<Hub> _hub;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<OrderWorkflowService> _logger;

    public OrderWorkflowService(
        IApplicationDbContext db,
        IHubContext<Hub> hub,
        UserManager<AppUser> userManager,
        ILogger<OrderWorkflowService> logger)
    {
        _db = db; _hub = hub; _userManager = userManager; _logger = logger;
    }

    public async Task<(List<OrderAdminListItemDto> Items, int Total)> GetForAdminAsync(
        OrderAdminFilterDto filter)
    {
        var q = _db.Orders.Include(o => o.Items).AsQueryable();

        if (filter.Status.HasValue) q = q.Where(o => o.Status == filter.Status.Value);
        if (filter.PaymentMethod.HasValue) q = q.Where(o => o.PaymentMethod == filter.PaymentMethod.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s) || o.ReceiverName.Contains(s) ||
                             o.ReceiverPhone.Contains(s) ||
                             (o.TrackingNumber != null && o.TrackingNumber.Contains(s)));
        }

        if (filter.FromDate.HasValue) q = q.Where(o => o.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(o => o.CreatedAt <= filter.ToDate.Value);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new OrderAdminListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                ReceiverName = o.ReceiverName,
                ReceiverPhone = o.ReceiverPhone,
                City = o.City,
                TotalAmount = o.TotalAmount,
                ItemCount = o.Items.Sum(i => i.Quantity),
                Status = o.Status,
                StatusLabel = GetStatusLabel(o.Status),
                PaymentMethod = o.PaymentMethod,
                ShippingMethod = o.ShippingMethod,
                ShippingCompany = o.ShippingCompany,
                TrackingNumber = o.TrackingNumber,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToListAsync();

        foreach (var item in items) ApplyActionFlags(item);
        return (items, total);
    }

    public async Task<int> GetPendingCountAsync()
        => await _db.Orders.CountAsync(o =>
            o.Status == OrderStatus.Pending ||
            o.Status == OrderStatus.Paid ||
            o.Status == OrderStatus.Processing);

    public async Task<OrderAdminDetailDto?> GetAdminDetailAsync(int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        var userIds = new List<int>();
        if (order.PreparedByUserId.HasValue) userIds.Add(order.PreparedByUserId.Value);
        if (order.PackedByUserId.HasValue) userIds.Add(order.PackedByUserId.Value);
        if (order.ShippedByUserId.HasValue) userIds.Add(order.ShippedByUserId.Value);

        var userNames = userIds.Any()
            ? await _db.Users.Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = $"{u.FirstName} {u.LastName}".Trim() })
                .ToDictionaryAsync(u => u.Id, u => u.Name)
            : new Dictionary<int, string>();

        var dto = new OrderAdminDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            StatusLabel = GetStatusLabel(order.Status),
            StatusStep = GetStatusStep(order.Status),
            PaymentMethod = order.PaymentMethod,
            PaymentMethodLabel = order.PaymentMethod == PaymentMethod.Online
                ? "پرداخت آنلاین" : "پرداخت درب منزل",
            ShippingMethod = order.ShippingMethod,
            ShippingMethodLabel = order.ShippingMethod switch
            {
                ShippingMethod.Normal => "ارسال عادی",
                ShippingMethod.Express => "ارسال سریع",
                ShippingMethod.Pickup => "تحویل حضوری",
                _ => ""
            },
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            ProcessingAt = order.ProcessingAt,
            PackedAt = order.PackedAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            PreparedByUserName = order.PreparedByUserId.HasValue
                ? userNames.GetValueOrDefault(order.PreparedByUserId.Value) : null,
            PackedByUserName = order.PackedByUserId.HasValue
                ? userNames.GetValueOrDefault(order.PackedByUserId.Value) : null,
            ShippedByUserName = order.ShippedByUserId.HasValue
                ? userNames.GetValueOrDefault(order.ShippedByUserId.Value) : null,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            Province = order.Province,
            City = order.City,
            PostalCode = order.PostalCode,
            FullAddress = order.FullAddress,
            SubTotal = order.SubTotal,
            ShippingCost = order.ShippingCost,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            PaymentTrackingCode = order.PaymentTrackingCode,
            ShippingCompany = order.ShippingCompany,
            TrackingNumber = order.TrackingNumber,
            InternalNote = order.InternalNote,
            CancelReason = order.CancelReason,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductSlug = i.Product?.Slug,
                ProductImage = i.Product?.Images.OrderBy(pi => pi.DisplayOrder)
                    .Select(pi => pi.Path).FirstOrDefault(),
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice
            }).ToList(),
            Timeline = order.StatusHistory.OrderByDescending(h => h.ChangedAt)
                .Select(h => new OrderTimelineItemDto
                {
                    Status = h.Status,
                    StatusLabel = GetStatusLabel(h.Status),
                    Note = h.Note,
                    ChangedAt = h.ChangedAt,
                    Icon = GetStatusIcon(h.Status),
                    Color = GetStatusColor(h.Status)
                }).ToList()
        };

        ApplyDetailFlags(dto);
        return dto;
    }

    public async Task<(bool Ok, string Message)> StartProcessingAsync(int orderId, int adminId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return (false, "سفارش یافت نشد");
        if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Pending)
            return (false, $"وضعیت فعلی ({GetStatusLabel(order.Status)}) اجازه پردازش نمی‌دهد");

        order.Status = OrderStatus.Processing;
        order.ProcessingAt = DateTime.UtcNow;
        order.PreparedByUserId = adminId;
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = OrderStatus.Processing,
            Note = "شروع پردازش توسط مدیر",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await NotifyStatusChangeAsync(order);
        return (true, "سفارش در حال پردازش قرار گرفت");
    }

    public async Task<(bool Ok, string Message)> MarkAsPackedAsync(
        int orderId, int adminId, string? note = null)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return (false, "سفارش یافت نشد");
        if (order.Status != OrderStatus.Processing)
            return (false, $"وضعیت فعلی: {GetStatusLabel(order.Status)}");

        order.Status = OrderStatus.Packed;
        order.PackedAt = DateTime.UtcNow;
        order.PackedByUserId = adminId;
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = OrderStatus.Packed,
            Note = string.IsNullOrWhiteSpace(note) ? "بسته‌بندی سفارش انجام شد" : $"بسته‌بندی: {note}",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await NotifyStatusChangeAsync(order);
        return (true, "سفارش بسته‌بندی شد");
    }

    public async Task<(bool Ok, string Message)> MarkAsShippedAsync(ShipOrderDto dto, int adminId)
    {
        var order = await _db.Orders.FindAsync(dto.OrderId);
        if (order == null) return (false, "سفارش یافت نشد");

        if (order.Status != OrderStatus.Packed && order.Status != OrderStatus.Processing)
            return (false, $"وضعیت فعلی: {GetStatusLabel(order.Status)}");

        if (string.IsNullOrWhiteSpace(dto.ShippingCompany))
            return (false, "شرکت حمل‌ونقل الزامی است");
        if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
            return (false, "کد رهگیری الزامی است");

        var duplicate = await _db.Orders.AnyAsync(o =>
            o.TrackingNumber == dto.TrackingNumber.Trim() && o.Id != dto.OrderId);
        if (duplicate)
            return (false, "این کد رهگیری قبلاً ثبت شده است");

        order.Status = OrderStatus.Shipped;
        order.ShippedAt = DateTime.UtcNow;
        order.ShippedByUserId = adminId;
        order.ShippingCompany = dto.ShippingCompany.Trim();
        order.TrackingNumber = dto.TrackingNumber.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = dto.OrderId,
            Status = OrderStatus.Shipped,
            Note = $"ارسال از طریق {order.ShippingCompany} - کد رهگیری: {order.TrackingNumber}" +
                   (string.IsNullOrWhiteSpace(dto.Note) ? "" : $" - {dto.Note}"),
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await NotifyStatusChangeAsync(order);
        _logger.LogInformation("Order {N} shipped via {C}",
            order.OrderNumber, order.ShippingCompany);
        return (true, "سفارش ارسال شد");
    }

    public async Task<(bool Ok, string Message)> MarkAsDeliveredAsync(int orderId, int adminId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return (false, "سفارش یافت نشد");
        if (order.Status != OrderStatus.Shipped)
            return (false, "سفارش باید در وضعیت «ارسال شده» باشد");

        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        order.DeliveredByUserId = adminId;
        order.UpdatedAt = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = orderId,
            Status = OrderStatus.Delivered,
            Note = "تحویل توسط مشتری تأیید شد",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await NotifyStatusChangeAsync(order);
        return (true, "تحویل تأیید شد");
    }

    public async Task<(bool Ok, string Message)> CancelAsync(CancelOrderDto dto, int adminId)
    {
        var order = await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order == null) return (false, "سفارش یافت نشد");

        if (order.Status == OrderStatus.Cancelled)
            return (false, "قبلاً لغو شده");
        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            return (false, "سفارش ارسال/تحویل‌شده قابل لغو نیست");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return (false, "دلیل لغو الزامی است");

        order.Status = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelReason = dto.Reason.Trim();
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null) product.Stock += item.Quantity;
        }

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = OrderStatus.Cancelled,
            Note = $"لغو توسط مدیر: {dto.Reason}",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await NotifyStatusChangeAsync(order);
        return (true, "سفارش لغو شد و موجودی بازگشت");
    }

    public async Task<(int Success, int Failed)> BulkPackAsync(
        IEnumerable<int> orderIds, int adminId)
    {
        int ok = 0, fail = 0;
        foreach (var id in orderIds)
        {
            var (s, _) = await MarkAsPackedAsync(id, adminId);
            if (s) ok++; else fail++;
        }
        return (ok, fail);
    }

    public async Task<(int Success, int Failed)> BulkShipAsync(
        IEnumerable<int> orderIds, string company, int adminId)
    {
        int ok = 0, fail = 0;
        foreach (var id in orderIds)
        {
            var (s, _) = await MarkAsShippedAsync(new ShipOrderDto
            {
                OrderId = id,
                ShippingCompany = company,
                TrackingNumber = $"BULK-{DateTime.UtcNow.Ticks}-{id}"
            }, adminId);
            if (s) ok++; else fail++;
        }
        return (ok, fail);
    }

    public async Task<OrderTrackingDto?> GetTrackingAsync(int orderId, int userId)
    {
        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

        return order == null ? null : MapTracking(order);
    }

    public async Task<List<OrderTrackingDto>> GetCustomerOrdersAsync(
        int userId, OrderStatus? status = null)
    {
        var q = _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p!.Images)
            .Include(o => o.StatusHistory)
            .Where(o => o.CustomerId == userId);

        if (status.HasValue) q = q.Where(o => o.Status == status.Value);

        var orders = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return orders.Select(MapTracking).ToList();
    }

    private static OrderTrackingDto MapTracking(Order o) => new()
    {
        Id = o.Id,
        OrderNumber = o.OrderNumber,
        Status = o.Status,
        StatusLabel = GetStatusLabel(o.Status),
        StatusStep = GetStatusStep(o.Status),
        CreatedAt = o.CreatedAt,
        PaidAt = o.PaidAt,
        PackedAt = o.PackedAt,
        ShippedAt = o.ShippedAt,
        DeliveredAt = o.DeliveredAt,
        ShippingCompany = o.ShippingCompany,
        TrackingNumber = o.TrackingNumber,
        TotalAmount = o.TotalAmount,
        ReceiverName = o.ReceiverName,
        ReceiverPhone = o.ReceiverPhone,
        FullAddress = $"{o.Province}، {o.City}، {o.FullAddress}",
        Items = o.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            ProductSlug = i.Product?.Slug,
            ProductImage = i.Product?.Images.OrderBy(pi => pi.DisplayOrder).Select(pi => pi.Path).FirstOrDefault(),
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            TotalPrice = i.TotalPrice
        }).ToList(),
        Timeline = o.StatusHistory.OrderByDescending(h => h.ChangedAt)
            .Select(h => new OrderTimelineItemDto
            {
                Status = h.Status,
                StatusLabel = GetStatusLabel(h.Status),
                Note = h.Note,
                ChangedAt = h.ChangedAt,
                Icon = GetStatusIcon(h.Status),
                Color = GetStatusColor(h.Status)
            }).ToList()
    };

    private async Task NotifyStatusChangeAsync(Order order)
    {
        try
        {
            await _hub.Clients.Group("admins").SendAsync("OrderUpdated", new
            {
                order.Id,
                order.OrderNumber,
                status = order.Status.ToString(),
                statusLabel = GetStatusLabel(order.Status),
                updatedAt = DateTime.UtcNow
            });

            if (order.CustomerId.HasValue)
            {
                await _hub.Clients.User(order.CustomerId.Value.ToString())
                    .SendAsync("OrderStatusChanged", new
                    {
                        order.Id,
                        order.OrderNumber,
                        status = order.Status.ToString(),
                        statusLabel = GetStatusLabel(order.Status),
                        trackingNumber = order.TrackingNumber,
                        shippingCompany = order.ShippingCompany,
                        updatedAt = DateTime.UtcNow
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR notify failed for order {Id}", order.Id);
        }
    }

    private static void ApplyActionFlags(OrderAdminListItemDto dto)
    {
        dto.CanProcess = dto.Status == OrderStatus.Paid || dto.Status == OrderStatus.Pending;
        dto.CanPack = dto.Status == OrderStatus.Processing;
        dto.CanShip = dto.Status == OrderStatus.Packed || dto.Status == OrderStatus.Processing;
        dto.CanDeliver = dto.Status == OrderStatus.Shipped;
        dto.CanCancel = dto.Status == OrderStatus.Pending || dto.Status == OrderStatus.Paid ||
                        dto.Status == OrderStatus.Processing || dto.Status == OrderStatus.Packed;
    }

    private static void ApplyDetailFlags(OrderAdminDetailDto dto)
    {
        dto.CanProcess = dto.Status == OrderStatus.Paid || dto.Status == OrderStatus.Pending;
        dto.CanPack = dto.Status == OrderStatus.Processing;
        dto.CanShip = dto.Status == OrderStatus.Packed || dto.Status == OrderStatus.Processing;
        dto.CanDeliver = dto.Status == OrderStatus.Shipped;
        dto.CanCancel = dto.Status == OrderStatus.Pending || dto.Status == OrderStatus.Paid ||
                        dto.Status == OrderStatus.Processing || dto.Status == OrderStatus.Packed;
    }

    public static string GetStatusLabel(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "در انتظار پرداخت",
        OrderStatus.Paid => "پرداخت شده",
        OrderStatus.Processing => "در حال پردازش",
        OrderStatus.Packed => "بسته‌بندی شده",
        OrderStatus.Shipped => "ارسال شده",
        OrderStatus.Delivered => "تحویل داده شده",
        OrderStatus.Cancelled => "لغو شده",
        OrderStatus.Returned => "مرجوعی",
        _ => s.ToString()
    };

    public static int GetStatusStep(OrderStatus s) => s switch
    {
        OrderStatus.Pending => 0,
        OrderStatus.Paid => 1,
        OrderStatus.Processing => 2,
        OrderStatus.Packed => 3,
        OrderStatus.Shipped => 4,
        OrderStatus.Delivered => 5,
        OrderStatus.Cancelled => -1,
        OrderStatus.Returned => -1,
        _ => 0
    };

    public static string GetStatusIcon(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "w-icon-clock",
        OrderStatus.Paid => "w-icon-money",
        OrderStatus.Processing => "w-icon-tools",
        OrderStatus.Packed => "w-icon-gift",
        OrderStatus.Shipped => "w-icon-truck",
        OrderStatus.Delivered => "w-icon-check-solid",
        OrderStatus.Cancelled => "w-icon-times-circle",
        _ => "w-icon-circle"
    };

    public static string GetStatusColor(OrderStatus s) => s switch
    {
        OrderStatus.Pending => "#e67e22",
        OrderStatus.Paid => "#00b16b",
        OrderStatus.Processing => "#0088cc",
        OrderStatus.Packed => "#8e44ad",
        OrderStatus.Shipped => "#3498db",
        OrderStatus.Delivered => "#00b16b",
        OrderStatus.Cancelled => "#e74c3c",
        _ => "#64748b"
    };
}