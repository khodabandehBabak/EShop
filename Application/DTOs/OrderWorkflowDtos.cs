using System.ComponentModel.DataAnnotations;
using EShop.Domain.Enums;

namespace EShop.Application.DTOs;

// ============ Action Inputs ============
public class ShipOrderDto
{
    [Required]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "شرکت حمل‌ونقل الزامی است")]
    [MaxLength(100)]
    [Display(Name = "شرکت حمل‌ونقل")]
    public string ShippingCompany { get; set; } = "";

    [Required(ErrorMessage = "کد رهگیری الزامی است")]
    [MaxLength(100)]
    [Display(Name = "کد رهگیری")]
    public string TrackingNumber { get; set; } = "";

    [MaxLength(500)]
    [Display(Name = "یادداشت")]
    public string? Note { get; set; }
}

public class CancelOrderDto
{
    public int OrderId { get; set; }

    [Required(ErrorMessage = "دلیل لغو الزامی است")]
    [MaxLength(500)]
    public string Reason { get; set; } = "";
}

// ============ Admin List ============
public class OrderAdminListItemDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string ReceiverName { get; set; } = "";
    public string ReceiverPhone { get; set; } = "";
    public string City { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public OrderStatus Status { get; set; }
    public string StatusLabel { get; set; } = "";
    public PaymentMethod PaymentMethod { get; set; }
    public ShippingMethod ShippingMethod { get; set; }
    public string? ShippingCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool CanProcess { get; set; }
    public bool CanPack { get; set; }
    public bool CanShip { get; set; }
    public bool CanDeliver { get; set; }
    public bool CanCancel { get; set; }
}

public class OrderAdminFilterDto
{
    public OrderStatus? Status { get; set; }
    public string? Search { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ============ Admin Detail ============
public class OrderAdminDetailDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public OrderStatus Status { get; set; }
    public string StatusLabel { get; set; } = "";
    public int StatusStep { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodLabel { get; set; } = "";
    public ShippingMethod ShippingMethod { get; set; }
    public string ShippingMethodLabel { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ProcessingAt { get; set; }
    public DateTime? PackedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public string? PreparedByUserName { get; set; }
    public string? PackedByUserName { get; set; }
    public string? ShippedByUserName { get; set; }

    public string ReceiverName { get; set; } = "";
    public string ReceiverPhone { get; set; } = "";
    public string Province { get; set; } = "";
    public string City { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string FullAddress { get; set; } = "";

    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? PaymentTrackingCode { get; set; }
    public string? ShippingCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public string? InternalNote { get; set; }
    public string? CancelReason { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderTimelineItemDto> Timeline { get; set; } = new();

    public bool CanProcess { get; set; }
    public bool CanPack { get; set; }
    public bool CanShip { get; set; }
    public bool CanDeliver { get; set; }
    public bool CanCancel { get; set; }
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? ProductImage { get; set; }
    public string? ProductSlug { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderTimelineItemDto
{
    public OrderStatus Status { get; set; }
    public string StatusLabel { get; set; } = "";
    public string? Note { get; set; }
    public string? UserName { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
}

// ============ Customer Tracking ============
public class OrderTrackingDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public OrderStatus Status { get; set; }
    public string StatusLabel { get; set; } = "";
    public int StatusStep { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? PackedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public string? ShippingCompany { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal TotalAmount { get; set; }

    public List<OrderTimelineItemDto> Timeline { get; set; } = new();
    public List<OrderItemDto> Items { get; set; } = new();

    public string ReceiverName { get; set; } = "";
    public string ReceiverPhone { get; set; } = "";
    public string FullAddress { get; set; } = "";
}