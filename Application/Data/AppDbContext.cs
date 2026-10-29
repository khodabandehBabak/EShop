//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore;
//using EShop.Domain.Entities;

//namespace EShop.Application.Data;

//public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
//{
//    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

//    // ============ Product ============
//    public DbSet<Product> Products => Set<Product>();
//    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
//    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
//    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
//    public DbSet<ProductAttributeOption> ProductAttributeOptions => Set<ProductAttributeOption>();
//    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
//    public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues => Set<ProductVariantAttributeValue>();

//    // ============ Category & Brand ============
//    public DbSet<Category> Categories => Set<Category>();
//    public DbSet<Brand> Brands => Set<Brand>();

//    // ============ Order ============
//    public DbSet<Order> Orders => Set<Order>();
//    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
//    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

//    // ============ Identity & Permissions ============
//    public DbSet<Permission> Permissions => Set<Permission>();
//    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
//    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

//    // ============ User Related ============
//    public DbSet<Address> Addresses => Set<Address>();
//    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
//    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

//    // ============ Review ============
//    public DbSet<Review> Reviews => Set<Review>();
//    public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();

//    // ============ Slider & Settings ============
//    public DbSet<Slider> Sliders => Set<Slider>();
//    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

//    protected override void OnModelCreating(ModelBuilder builder)
//    {
//        base.OnModelCreating(builder);

//        // ============================================================
//        // 1. Rename Identity tables to clean names
//        // ============================================================
//        builder.Entity<AppUser>().ToTable("Users");
//        builder.Entity<AppRole>().ToTable("Roles");
//        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
//        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
//        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
//        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
//        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

//        // ============================================================
//        // 2. Product Configuration
//        // ============================================================
//        builder.Entity<Product>(e =>
//        {
//            e.HasIndex(p => p.Slug).IsUnique();
//            e.HasIndex(p => p.Sku);
//            e.HasIndex(p => new { p.IsActive, p.ViewCount });
//            e.HasIndex(p => new { p.CategoryId, p.IsActive });
//            e.HasIndex(p => new { p.BrandId, p.IsActive });

//            e.Property(p => p.Name).HasMaxLength(250).IsRequired();
//            e.Property(p => p.Slug).HasMaxLength(250).IsRequired();
//            e.Property(p => p.Sku).HasMaxLength(100);
//            e.Property(p => p.Price).HasPrecision(18, 0);
//            e.Property(p => p.DiscountPrice).HasPrecision(18, 0);
//            e.Property(p => p.Weight).HasPrecision(10, 2);
//            e.Property(p => p.Dimensions).HasMaxLength(100);
//            e.Property(p => p.ShortDescription).HasMaxLength(500);
//            e.Property(p => p.MetaTitle).HasMaxLength(70);
//            e.Property(p => p.MetaDescription).HasMaxLength(160);
//            e.Property(p => p.MetaKeywords).HasMaxLength(300);

//            e.HasQueryFilter(p => !p.IsDeleted);

//            e.HasOne(p => p.Category)
//                .WithMany(c => c.Products)
//                .HasForeignKey(p => p.CategoryId)
//                .OnDelete(DeleteBehavior.Restrict);

//            e.HasOne(p => p.Brand)
//                .WithMany(b => b.Products)
//                .HasForeignKey(p => p.BrandId)
//                .OnDelete(DeleteBehavior.SetNull);
//        });

//        // ============================================================
//        // 3. ProductImage Configuration
//        // ============================================================
//        builder.Entity<ProductImage>(e =>
//        {
//            e.HasIndex(i => new { i.ProductId, i.DisplayOrder });
//            e.Property(i => i.Path).HasMaxLength(500).IsRequired();
//            e.Property(i => i.Alt).HasMaxLength(200);

//            e.HasOne(i => i.Product)
//                .WithMany(p => p.Images)
//                .HasForeignKey(i => i.ProductId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        // ============================================================
//        // 4. ProductVariant Configuration
//        // ============================================================
//        builder.Entity<ProductVariant>(e =>
//        {
//            e.HasIndex(v => v.Sku).IsUnique();
//            e.HasIndex(v => new { v.ProductId, v.IsActive });

//            e.Property(v => v.Sku).HasMaxLength(100).IsRequired();
//            e.Property(v => v.Price).HasPrecision(18, 0);
//            e.Property(v => v.DiscountPrice).HasPrecision(18, 0);
//            e.Property(v => v.ImageUrl).HasMaxLength(500);

//            e.HasOne(v => v.Product)
//                .WithMany(p => p.Variants)
//                .HasForeignKey(v => v.ProductId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        // ============================================================
//        // 5. ProductAttribute Configuration
//        // ============================================================
//        builder.Entity<ProductAttribute>(e =>
//        {
//            e.HasIndex(a => a.Slug).IsUnique();
//            e.Property(a => a.Name).HasMaxLength(100).IsRequired();
//            e.Property(a => a.Slug).HasMaxLength(100).IsRequired();
//        });

//        builder.Entity<ProductAttributeOption>(e =>
//        {
//            e.HasIndex(o => new { o.AttributeId, o.DisplayOrder });
//            e.Property(o => o.Value).HasMaxLength(100).IsRequired();
//            e.Property(o => o.ColorCode).HasMaxLength(10);

//            e.HasOne(o => o.Attribute)
//                .WithMany(a => a.Options)
//                .HasForeignKey(o => o.AttributeId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        builder.Entity<ProductAttributeValue>(e =>
//        {
//            e.HasIndex(pav => new { pav.ProductId, pav.AttributeId }).IsUnique();

//            e.HasOne(pav => pav.Product)
//                .WithMany(p => p.AttributeValues)
//                .HasForeignKey(pav => pav.ProductId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(pav => pav.Attribute)
//                .WithMany()
//                .HasForeignKey(pav => pav.AttributeId)
//                .OnDelete(DeleteBehavior.Restrict);

//            e.HasOne(pav => pav.Option)
//                .WithMany()
//                .HasForeignKey(pav => pav.OptionId)
//                .OnDelete(DeleteBehavior.Restrict);
//        });

//        builder.Entity<ProductVariantAttributeValue>(e =>
//        {
//            e.HasIndex(pvav => new { pvav.VariantId, pvav.AttributeId }).IsUnique();

//            e.HasOne(pvav => pvav.Variant)
//                .WithMany(v => v.AttributeValues)
//                .HasForeignKey(pvav => pvav.VariantId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(pvav => pvav.Attribute)
//                .WithMany()
//                .HasForeignKey(pvav => pvav.AttributeId)
//                .OnDelete(DeleteBehavior.Restrict);

//            e.HasOne(pvav => pvav.Option)
//                .WithMany()
//                .HasForeignKey(pvav => pvav.OptionId)
//                .OnDelete(DeleteBehavior.Restrict);
//        });

//        // ============================================================
//        // 6. Category Configuration
//        // ============================================================
//        builder.Entity<Category>(e =>
//        {
//            e.HasIndex(c => c.Slug).IsUnique();
//            e.HasIndex(c => new { c.ParentId, c.DisplayOrder });

//            e.Property(c => c.Name).HasMaxLength(150).IsRequired();
//            e.Property(c => c.Slug).HasMaxLength(150).IsRequired();
//            e.Property(c => c.Description).HasMaxLength(500);
//            e.Property(c => c.ImageUrl).HasMaxLength(500);
//            e.Property(c => c.Icon).HasMaxLength(100);
//            e.Property(c => c.MetaTitle).HasMaxLength(70);
//            e.Property(c => c.MetaDescription).HasMaxLength(160);
//            e.Property(c => c.MetaKeywords).HasMaxLength(300);

//            e.HasQueryFilter(c => !c.IsDeleted);

//            e.HasOne(c => c.Parent)
//                .WithMany(c => c.Children)
//                .HasForeignKey(c => c.ParentId)
//                .OnDelete(DeleteBehavior.Restrict);
//        });

//        // ============================================================
//        // 7. Brand Configuration
//        // ============================================================
//        builder.Entity<Brand>(e =>
//        {
//            e.HasIndex(b => b.Slug).IsUnique();
//            e.Property(b => b.Name).HasMaxLength(150).IsRequired();
//            e.Property(b => b.Slug).HasMaxLength(150).IsRequired();
//            e.Property(b => b.Description).HasMaxLength(1000);
//            e.Property(b => b.LogoUrl).HasMaxLength(500);
//            e.Property(b => b.Website).HasMaxLength(200);
//            e.Property(b => b.MetaTitle).HasMaxLength(70);
//            e.Property(b => b.MetaDescription).HasMaxLength(160);

//            e.HasQueryFilter(b => !b.IsDeleted);
//        });

//        // ============================================================
//        // 8. Order Configuration
//        // ============================================================
//        builder.Entity<Order>(e =>
//        {
//            e.HasIndex(o => o.OrderNumber).IsUnique();
//            e.HasIndex(o => new { o.Status, o.CreatedAt });
//            e.HasIndex(o => o.CustomerId);
//            e.HasIndex(o => o.TrackingNumber);

//            e.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
//            e.Property(o => o.ReceiverName).HasMaxLength(100).IsRequired();
//            e.Property(o => o.ReceiverPhone).HasMaxLength(20).IsRequired();
//            e.Property(o => o.Province).HasMaxLength(100);
//            e.Property(o => o.City).HasMaxLength(100);
//            e.Property(o => o.PostalCode).HasMaxLength(20);
//            e.Property(o => o.FullAddress).HasMaxLength(500).IsRequired();
//            e.Property(o => o.ShippingCompany).HasMaxLength(100);
//            e.Property(o => o.TrackingNumber).HasMaxLength(100);
//            e.Property(o => o.CancelReason).HasMaxLength(500);
//            e.Property(o => o.InternalNote).HasMaxLength(2000);

//            e.Property(o => o.SubTotal).HasPrecision(18, 0);
//            e.Property(o => o.ShippingCost).HasPrecision(18, 0);
//            e.Property(o => o.DiscountAmount).HasPrecision(18, 0);
//            e.Property(o => o.TotalAmount).HasPrecision(18, 0);

//            e.HasQueryFilter(o => !o.IsDeleted);

//            e.HasOne(o => o.Customer)
//                .WithMany(u => u.Orders)
//                .HasForeignKey(o => o.CustomerId)
//                .OnDelete(DeleteBehavior.SetNull);
//        });

//        builder.Entity<OrderItem>(e =>
//        {
//            e.Property(oi => oi.ProductName).HasMaxLength(250).IsRequired();
//            e.Property(oi => oi.UnitPrice).HasPrecision(18, 0);
//            e.Property(oi => oi.TotalPrice).HasPrecision(18, 0);

//            e.HasOne(oi => oi.Order)
//                .WithMany(o => o.Items)
//                .HasForeignKey(oi => oi.OrderId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(oi => oi.Product)
//                .WithMany(p => p.OrderItems)
//                .HasForeignKey(oi => oi.ProductId)
//                .OnDelete(DeleteBehavior.Restrict);
//        });

//        builder.Entity<OrderStatusHistory>(e =>
//        {
//            e.HasIndex(h => new { h.OrderId, h.ChangedAt });
//            e.Property(h => h.Note).HasMaxLength(500);

//            e.HasOne(h => h.Order)
//                .WithMany(o => o.StatusHistory)
//                .HasForeignKey(h => h.OrderId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        // ============================================================
//        // 9. Permission Configuration
//        // ============================================================
//        builder.Entity<Permission>(e =>
//        {
//            e.HasIndex(p => p.Code).IsUnique();
//            e.Property(p => p.Code).HasMaxLength(100).IsRequired();
//            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
//            e.Property(p => p.Group).HasMaxLength(100).IsRequired();
//            e.Property(p => p.Description).HasMaxLength(500);
//        });

//        builder.Entity<RolePermission>(e =>
//        {
//            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });

//            e.HasOne(rp => rp.Role)
//                .WithMany()
//                .HasForeignKey(rp => rp.RoleId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(rp => rp.Permission)
//                .WithMany(p => p.RolePermissions)
//                .HasForeignKey(rp => rp.PermissionId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        builder.Entity<UserPermission>(e =>
//        {
//            e.HasKey(up => new { up.UserId, up.PermissionId });

//            e.HasOne(up => up.User)
//                .WithMany()
//                .HasForeignKey(up => up.UserId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(up => up.Permission)
//                .WithMany(p => p.UserPermissions)
//                .HasForeignKey(up => up.PermissionId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        // ============================================================
//        // 10. AppUser Configuration
//        // ============================================================
//        builder.Entity<AppUser>(e =>
//        {
//            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
//            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
//            e.Property(u => u.NationalCode).HasMaxLength(10);
//            e.HasIndex(u => u.PhoneNumber);
//        });

//        builder.Entity<AppRole>(e =>
//        {
//            e.Property(r => r.Description).HasMaxLength(500);
//        });

//        // ============================================================
//        // 11. Address Configuration
//        // ============================================================
//        builder.Entity<Address>(e =>
//        {
//            e.Property(a => a.ReceiverName).HasMaxLength(100).IsRequired();
//            e.Property(a => a.Phone).HasMaxLength(20).IsRequired();
//            e.Property(a => a.Province).HasMaxLength(100);
//            e.Property(a => a.City).HasMaxLength(100);
//            e.Property(a => a.PostalCode).HasMaxLength(20);
//            e.Property(a => a.FullAddress).HasMaxLength(500).IsRequired();

//            e.HasOne(a => a.User)
//                .WithMany(u => u.Addresses)
//                .HasForeignKey(a => a.UserId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        // ============================================================
//        // 12. WishlistItem Configuration
//        // ============================================================
//        builder.Entity<WishlistItem>(e =>
//        {
//            e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();

//            e.HasOne(w => w.User)
//                .WithMany(u => u.Wishlist)
//                .HasForeignKey(w => w.UserId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(w => w.Product)
//                .WithMany()
//                .HasForeignKey(w => w.ProductId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        // ============================================================
//        // 13. OtpCode Configuration
//        // ============================================================
//        builder.Entity<OtpCode>(e =>
//        {
//            e.HasIndex(o => new { o.PhoneNumber, o.Code, o.IsUsed });
//            e.Property(o => o.PhoneNumber).HasMaxLength(20).IsRequired();
//            e.Property(o => o.Code).HasMaxLength(10).IsRequired();
//        });

//        // ============================================================
//        // 14. Review Configuration
//        // ============================================================
//        builder.Entity<Review>(e =>
//        {
//            e.HasIndex(r => new { r.ProductId, r.IsApproved });
//            e.HasIndex(r => new { r.UserId, r.ProductId }).IsUnique();
//            e.HasIndex(r => new { r.IsApproved, r.CreatedAt });

//            e.Property(r => r.Title).HasMaxLength(100);
//            e.Property(r => r.Comment).HasMaxLength(2000);
//            e.Property(r => r.RejectionReason).HasMaxLength(500);

//            e.HasQueryFilter(r => !r.IsDeleted);

//            e.HasOne(r => r.Product)
//                .WithMany(p => p.Reviews)
//                .HasForeignKey(r => r.ProductId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(r => r.User)
//                .WithMany()
//                .HasForeignKey(r => r.UserId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        builder.Entity<ReviewVote>(e =>
//        {
//            e.HasIndex(v => new { v.ReviewId, v.UserId }).IsUnique();

//            e.HasOne(v => v.Review)
//                .WithMany(r => r.Votes)
//                .HasForeignKey(v => v.ReviewId)
//                .OnDelete(DeleteBehavior.Cascade);

//            e.HasOne(v => v.User)
//                .WithMany()
//                .HasForeignKey(v => v.UserId)
//                .OnDelete(DeleteBehavior.Cascade);
//        });

//        // ============================================================
//        // 15. Slider Configuration
//        // ============================================================
//        builder.Entity<Slider>(e =>
//        {
//            e.HasIndex(s => new { s.Position, s.DisplayOrder });
//            e.HasIndex(s => new { s.IsActive, s.Position });

//            e.Property(s => s.Title).HasMaxLength(200).IsRequired();
//            e.Property(s => s.Subtitle).HasMaxLength(300);
//            e.Property(s => s.Description).HasMaxLength(500);
//            e.Property(s => s.ImageUrl).HasMaxLength(500).IsRequired();
//            e.Property(s => s.ImageMobileUrl).HasMaxLength(500);
//            e.Property(s => s.ProductImageUrl).HasMaxLength(500);
//            e.Property(s => s.BtnText).HasMaxLength(50);
//            e.Property(s => s.BtnLink).HasMaxLength(500);
//            e.Property(s => s.BgColor).HasMaxLength(20);
//            e.Property(s => s.TextColor).HasMaxLength(20);
//            e.Property(s => s.Alt).HasMaxLength(200);

//            e.HasQueryFilter(s => !s.IsDeleted);
//        });

//        // ============================================================
//        // 16. SiteSetting Configuration
//        // ============================================================
//        builder.Entity<SiteSetting>(e =>
//        {
//            e.HasIndex(s => s.Key).IsUnique();
//            e.Property(s => s.Key).HasMaxLength(100).IsRequired();
//            e.Property(s => s.Value).HasColumnType("nvarchar(max)");
//            e.Property(s => s.Description).HasMaxLength(500);
//        });

//        // ============================================================
//        // 17. Seed Data — Roles
//        // ============================================================
//        builder.Entity<AppRole>().HasData(
//            new AppRole { Id = 1, Name = "SuperAdmin", NormalizedName = "SUPERADMIN", Description = "مدیر ارشد سیستم" },
//            new AppRole { Id = 2, Name = "Admin", NormalizedName = "ADMIN", Description = "مدیر فروشگاه" },
//            new AppRole { Id = 3, Name = "Customer", NormalizedName = "CUSTOMER", Description = "مشتری" }
//        );
//    }
//}