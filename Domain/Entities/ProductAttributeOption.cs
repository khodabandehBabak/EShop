using Domain.Common;

namespace EShop.Domain.Entities;

public class ProductAttributeOption : BaseEntity
{
    public int AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public string? ColorCode { get; set; }
    public int DisplayOrder { get; set; }
}