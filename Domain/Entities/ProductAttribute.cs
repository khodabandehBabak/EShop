using Domain.Common;
using EShop.Domain.Enums;

namespace EShop.Domain.Entities;

public class ProductAttribute : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public AttributeType Type { get; set; } = AttributeType.Select;
    public int DisplayOrder { get; set; }
    public bool IsFilterable { get; set; } = true;

    public ICollection<ProductAttributeOption> Options { get; set; }
        = new List<ProductAttributeOption>();
}