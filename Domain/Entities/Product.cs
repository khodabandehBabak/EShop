using Domain.Common;

namespace EShop.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public int? LowStockThreshold { get; set; } = 5;
    public decimal? Weight { get; set; }
    public string? Dimensions { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;
    public bool IsSpecialOffer { get; set; } = false;
    public bool HasVariants { get; set; } = false;

    public int ViewCount { get; set; } = 0;
    public DateTime? LastViewAt { get; set; }

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}