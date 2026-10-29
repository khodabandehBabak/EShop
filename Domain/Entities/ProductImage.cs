using Domain.Common;

namespace EShop.Domain.Entities;

public class ProductImage : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Path { get; set; } = string.Empty;
    public string? Alt { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsMain { get; set; }
}