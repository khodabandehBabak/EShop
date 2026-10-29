using Domain.Common;

namespace EShop.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? Icon { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool ShowInMenu { get; set; } = true;

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}