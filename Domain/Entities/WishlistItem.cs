using Domain.Common;

namespace EShop.Domain.Entities;

public class WishlistItem : BaseEntity
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}