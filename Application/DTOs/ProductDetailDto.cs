using EShop.Domain.Enums;

namespace EShop.Application.DTOs;

/// <summary>
/// DTO صفحه جزئیات محصول (Storefront)
/// </summary>
public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Sku { get; set; }
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int DiscountPercent { get; set; }
    public int Stock { get; set; }
    public bool HasVariants { get; set; }
    public decimal? Weight { get; set; }
    public string? Dimensions { get; set; }

    public bool IsInStock { get; set; }
    public bool IsLowStock { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategorySlug { get; set; } = "";
    public string? BrandName { get; set; }
    public string? BrandSlug { get; set; }
    public string? BrandLogoUrl { get; set; }

    public double? AvgRating { get; set; }
    public int ReviewCount { get; set; }
    public int Rating5Count { get; set; }
    public int Rating4Count { get; set; }
    public int Rating3Count { get; set; }
    public int Rating2Count { get; set; }
    public int Rating1Count { get; set; }

    public List<ProductImageDto> Images { get; set; } = new();
    public List<AttributeSpecDto> Specs { get; set; } = new();
    public List<VariantAttributeGroupDto> AttributeGroups { get; set; } = new();
    public List<VariantDto> Variants { get; set; } = new();

    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
}

public class ProductImageDto
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public string? Alt { get; set; }
    public bool IsMain { get; set; }
}

public class VariantAttributeGroupDto
{
    public int AttributeId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public AttributeType Type { get; set; }
    public List<VariantAttributeOptionDto> Options { get; set; } = new();
}

public class VariantAttributeOptionDto
{
    public int OptionId { get; set; }
    public string Value { get; set; } = "";
    public string? ColorCode { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class VariantDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
    public Dictionary<int, int> AttributeValues { get; set; } = new();
}