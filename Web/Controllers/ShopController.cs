using Microsoft.AspNetCore.Mvc;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;

namespace EShop.Web.Controllers;

public class ShopController : Controller
{
    private readonly IShopService _shop;
    public ShopController(IShopService shop) => _shop = shop;

    [HttpGet("/shop")]
    public async Task<IActionResult> Index(
        string? search, string? category, string? brand,
        decimal? minPrice, decimal? maxPrice, string? attrs,
        bool? inStock, bool? discounted, string? sort, int page = 1)
    {
        var filter = BuildFilter(search, category, brand, minPrice, maxPrice,
            attrs, inStock, discounted, sort, page);

        var result = await _shop.GetProductsAsync(filter);

        ViewData["Title"] = result.PageTitle
            ?? (string.IsNullOrWhiteSpace(search) ? "فروشگاه" : $"جستجو: {search}");
        ViewData["Description"] = result.PageDescription
            ?? "خرید آنلاین از فروشگاه ستارخان با بهترین قیمت، ارسال سریع و ضمانت بازگشت کالا.";

        return View(result);
    }

    [HttpGet("/category/{slug}")]
    public async Task<IActionResult> Category(
        string slug, string? brand, decimal? minPrice, decimal? maxPrice,
        string? attrs, bool? inStock, bool? discounted, string? sort, int page = 1)
    {
        var filter = BuildFilter(null, slug, brand, minPrice, maxPrice,
            attrs, inStock, discounted, sort, page);
        var result = await _shop.GetProductsForCategoryAsync(slug, filter);

        ViewData["Title"] = result.PageTitle ?? "دسته‌بندی";
        ViewData["Description"] = result.PageDescription ?? "";

        return View("Index", result);
    }

    [HttpGet("/brand/{slug}")]
    public async Task<IActionResult> Brand(
        string slug, string? category, decimal? minPrice, decimal? maxPrice,
        string? attrs, bool? inStock, bool? discounted, string? sort, int page = 1)
    {
        var filter = BuildFilter(null, category, slug, minPrice, maxPrice,
            attrs, inStock, discounted, sort, page);
        var result = await _shop.GetProductsForBrandAsync(slug, filter);

        ViewData["Title"] = $"محصولات برند {slug}";
        return View("Index", result);
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static ShopFilterDto BuildFilter(
        string? search, string? category, string? brand,
        decimal? minPrice, decimal? maxPrice, string? attrs,
        bool? inStock, bool? discounted, string? sort, int page)
    {
        return new ShopFilterDto
        {
            Search = search,
            Categories = ParseList(category),
            Brands = ParseList(brand),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Attributes = ParseAttributes(attrs),
            OnlyInStock = inStock,
            OnlyDiscounted = discounted,
            SortBy = ParseSort(sort),
            Page = Math.Max(1, page)
        };
    }

    private static List<string> ParseList(string? input)
        => string.IsNullOrWhiteSpace(input)
            ? new List<string>()
            : input.Split(',', StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries).Distinct().ToList();

    private static Dictionary<int, List<int>> ParseAttributes(string? input)
    {
        var result = new Dictionary<int, List<int>>();
        if (string.IsNullOrWhiteSpace(input)) return result;

        foreach (var pair in input.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split(':');
            if (parts.Length != 2) continue;
            if (!int.TryParse(parts[0], out var aid)) continue;
            if (!int.TryParse(parts[1], out var oid)) continue;

            if (!result.ContainsKey(aid)) result[aid] = new List<int>();
            if (!result[aid].Contains(oid)) result[aid].Add(oid);
        }
        return result;
    }

    private static ShopSortBy ParseSort(string? input)
        => input?.ToLowerInvariant() switch
        {
            "oldest" => ShopSortBy.Oldest,
            "price_asc" or "priceasc" => ShopSortBy.PriceAsc,
            "price_desc" or "pricedesc" => ShopSortBy.PriceDesc,
            "best_selling" or "bestselling" => ShopSortBy.BestSelling,
            "top_rated" or "toprated" => ShopSortBy.TopRated,
            "most_discounted" or "mostdiscounted" => ShopSortBy.MostDiscounted,
            "most_viewed" or "mostviewed" => ShopSortBy.MostViewed,
            _ => ShopSortBy.Newest
        };
}