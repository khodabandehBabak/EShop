namespace EShop.Application.DTOs;

/// <summary>
/// کارت محصول — در همه‌جا استفاده می‌شود (صفحه اصلی، فروشگاه، سبد، ...)
/// </summary>
public class ProductCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? HoverImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int DiscountPercent { get; set; }
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsNew { get; set; }
    public int Stock { get; set; }
    public string? BrandName { get; set; }
    public string CategoryName { get; set; } = "";
    public int ViewCount { get; set; }
}

/// <summary>
/// مشخصات فنی نمایشی محصول (برای تب مشخصات)
/// </summary>
public class AttributeSpecDto
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string? ColorCode { get; set; }
}

/// <summary>
/// صفحه‌بندی عمومی
/// </summary>
public class PaginationDto
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 0;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}