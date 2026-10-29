using Domain.Common;

namespace EShop.Domain.Entities;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }

    public ICollection<ProductVariantAttributeValue> AttributeValues { get; set; }
        = new List<ProductVariantAttributeValue>();
}