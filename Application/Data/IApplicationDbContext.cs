using EShop.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Data;

/// <summary>
/// Interface برای DbContext — Application فقط از این استفاده می‌کند
/// پیاده‌سازی واقعی در Infrastructure است
/// </summary>
public interface IApplicationDbContext
{
    // Product
    DbSet<Product> Products { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductAttribute> ProductAttributes { get; }
    DbSet<ProductAttributeOption> ProductAttributeOptions { get; }
    DbSet<ProductAttributeValue> ProductAttributeValues { get; }
    DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues { get; }

    // Category & Brand
    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }

    // Order
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistories { get; }

    // Identity (فقط DbSetهای مورد نیاز)
    DbSet<AppUser> Users { get; }
    DbSet<AppRole> Roles { get; }
    DbSet<IdentityUserRole<int>> UserRoles { get; }

    // Permission
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserPermission> UserPermissions { get; }

    // User Related
    DbSet<Address> Addresses { get; }
    DbSet<WishlistItem> WishlistItems { get; }
    DbSet<OtpCode> OtpCodes { get; }

    // Review
    DbSet<Review> Reviews { get; }
    DbSet<ReviewVote> ReviewVotes { get; }

    // Slider & Settings
    DbSet<Slider> Sliders { get; }
    DbSet<SiteSetting> SiteSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}