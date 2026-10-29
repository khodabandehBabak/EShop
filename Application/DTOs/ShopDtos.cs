using EShop.Domain.Enums;

namespace EShop.Application.DTOs;

public enum ShopSortBy
{
    Newest = 0,
    Oldest = 1,
    PriceAsc = 2,
    PriceDesc = 3,
    BestSelling = 4,
    TopRated = 5,
    MostDiscounted = 6,
    MostViewed = 7
}

public class ShopFilterDto
{
    public string? Search { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<string> Brands { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public Dictionary<int, List<int>> Attributes { get; set; } = new();
    public bool? OnlyInStock { get; set; }
    public bool? OnlyDiscounted { get; set; }
    public ShopSortBy SortBy { get; set; } = ShopSortBy.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public bool HasAnyFilter =>
        !string.IsNullOrWhiteSpace(Search) ||
        Categories.Any() || Brands.Any() ||
        MinPrice.HasValue || MaxPrice.HasValue ||
        Attributes.Any(kv => kv.Value.Any()) ||
        OnlyInStock == true || OnlyDiscounted == true;
}

public class ShopResultDto
{
    public List<ProductCardDto> Products { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 0;
    public ShopFilterDto Filter { get; set; } = new();

    public List<CategoryFacetDto> Categories { get; set; } = new();
    public List<BrandFacetDto> Brands { get; set; } = new();
    public PriceRangeDto PriceRange { get; set; } = new();
    public List<AttributeFacetGroupDto> AttributeFacets { get; set; } = new();

    public string? PageTitle { get; set; }
    public string? PageDescription { get; set; }
}

public class CategoryFacetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int? ParentId { get; set; }
    public int ProductCount { get; set; }
    public bool IsSelected { get; set; }
    public int Level { get; set; }
}

public class BrandFacetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? LogoUrl { get; set; }
    public int ProductCount { get; set; }
    public bool IsSelected { get; set; }
}

public class PriceRangeDto
{
    public decimal MinAvailable { get; set; }
    public decimal MaxAvailable { get; set; }
    public decimal? MinSelected { get; set; }
    public decimal? MaxSelected { get; set; }
}

public class AttributeFacetGroupDto
{
    public int AttributeId { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public AttributeType Type { get; set; }
    public List<AttributeOptionFacetDto> Options { get; set; } = new();
}

public class AttributeOptionFacetDto
{
    public int OptionId { get; set; }
    public string Value { get; set; } = "";
    public string? ColorCode { get; set; }
    public int ProductCount { get; set; }
    public bool IsSelected { get; set; }
}