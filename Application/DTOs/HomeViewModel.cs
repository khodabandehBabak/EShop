namespace EShop.Application.DTOs;

/// <summary>
/// ViewModel صفحه اصلی فروشگاه
/// </summary>
public class HomeViewModel
{
    public List<SliderPublicDto> HeroSliders { get; set; } = new();
    public List<SliderPublicDto> MiddleBanners { get; set; } = new();

    public List<ProductCardDto> DealsOfTheDay { get; set; } = new();
    public List<ProductCardDto> TopSelling { get; set; } = new();
    public List<ProductCardDto> Newest { get; set; } = new();
    public List<ProductCardDto> Featured { get; set; } = new();
    public List<ProductCardDto> TopRated { get; set; } = new();
    public List<ProductCardDto> MostViewed { get; set; } = new();

    public List<CategoryWithImageDto> CategoryShowcase { get; set; } = new();
    public List<BrandCardDto> Brands { get; set; } = new();

    public decimal FreeShippingThreshold { get; set; } = 2_000_000m;
}

public class CategoryWithImageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Icon { get; set; }
    public int ProductCount { get; set; }
}

public class BrandCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? LogoUrl { get; set; }
    public int ProductCount { get; set; }
}