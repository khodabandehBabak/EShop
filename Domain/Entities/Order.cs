using Domain.Common;
using EShop.Domain.Enums;

namespace EShop.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public AppUser? Customer { get; set; }

    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public ShippingMethod ShippingMethod { get; set; }

    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? PaymentTrackingCode { get; set; }
    public DateTime? PaidAt { get; set; }

    public DateTime? ProcessingAt { get; set; }
    public int? PreparedByUserId { get; set; }

    public DateTime? PackedAt { get; set; }
    public int? PackedByUserId { get; set; }

    public DateTime? ShippedAt { get; set; }
    public int? ShippedByUserId { get; set; }
    public string? ShippingCompany { get; set; }
    public string? TrackingNumber { get; set; }

    public DateTime? DeliveredAt { get; set; }
    public int? DeliveredByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public string? InternalNote { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; }
        = new List<OrderStatusHistory>();
}