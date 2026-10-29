using Domain.Common;

namespace EShop.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
