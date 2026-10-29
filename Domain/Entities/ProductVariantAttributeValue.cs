using Domain.Common;

namespace EShop.Domain.Entities;

public class ProductVariantAttributeValue : BaseEntity
{
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = null!;
    public int OptionId { get; set; }
    public ProductAttributeOption Option { get; set; } = null!;
}