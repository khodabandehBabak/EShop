using Microsoft.AspNetCore.Identity;

namespace EShop.Domain.Entities;

public class AppUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? NationalCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<WishlistItem> Wishlist { get; set; } = new List<WishlistItem>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}