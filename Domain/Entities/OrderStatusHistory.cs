using Domain.Common;
using EShop.Domain.Enums;

namespace EShop.Domain.Entities;

public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}