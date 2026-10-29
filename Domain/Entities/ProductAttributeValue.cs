using Domain.Common;

namespace EShop.Domain.Entities;

public class ProductAttributeValue : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int AttributeId { get; set; }
    public ProductAttribute Attribute { get; set; } = null!;
    public int OptionId { get; set; }
    public ProductAttributeOption Option { get; set; } = null!;
}