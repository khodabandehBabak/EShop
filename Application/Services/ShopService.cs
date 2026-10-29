using EShop.Application.Data;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EShop.Application.Services;

public class ShopService : IShopService
{
    private readonly IApplicationDbContext _db;

    public ShopService(IApplicationDbContext db) => _db = db;

    public Task<ShopResultDto> GetProductsAsync(ShopFilterDto filter) => ExecuteAsync(filter);

    public Task<ShopResultDto> GetProductsForCategoryAsync(string categorySlug, ShopFilterDto filter)
    {
        filter.Categories = new List<string> { categorySlug };
        return ExecuteAsync(filter);
    }

    public Task<ShopResultDto> GetProductsForBrandAsync(string brandSlug, ShopFilterDto filter)
    {
        filter.Brands = new List<string> { brandSlug };
        return ExecuteAsync(filter);
    }

    public Task<ShopResultDto> SearchAsync(string query, ShopFilterDto filter)
    {
        filter.Search = query;
        return ExecuteAsync(filter);
    }

    private async Task<ShopResultDto> ExecuteAsync(ShopFilterDto filter)
    {
        var baseQuery = _db.Products.Where(p => p.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            baseQuery = baseQuery.Where(p => p.Name.Contains(s) ||
                (p.ShortDescription != null && p.ShortDescription.Contains(s)) ||
                (p.Sku != null && p.Sku.Contains(s)));
        }

        if (filter.OnlyInStock == true)
            baseQuery = baseQuery.Where(p => p.HasVariants
                ? p.Variants.Any(v => v.IsActive && v.Stock > 0) : p.Stock > 0);

        if (filter.OnlyDiscounted == true)
            baseQuery = baseQuery.Where(p => p.DiscountPrice != null);

        if (filter.Categories.Any())
        {
            var catIds = await GetCategoryIdsWithDescendantsAsync(filter.Categories);
            if (catIds.Any())
                baseQuery = baseQuery.Where(p => catIds.Contains(p.CategoryId));
            else
                baseQuery = baseQuery.Where(p => false);
        }

        if (filter.Brands.Any())
        {
            var brandIds = await _db.Brands
                .Where(b => filter.Brands.Contains(b.Slug))
                .Select(b => b.Id).ToListAsync();
            if (brandIds.Any())
                baseQuery = baseQuery.Where(p => p.BrandId.HasValue && brandIds.Contains(p.BrandId.Value));
            else
                baseQuery = baseQuery.Where(p => false);
        }

        if (filter.MinPrice.HasValue)
        {
            var min = filter.MinPrice.Value;
            baseQuery = baseQuery.Where(p => (p.DiscountPrice ?? p.Price) >= min);
        }
        if (filter.MaxPrice.HasValue)
        {
            var max = filter.MaxPrice.Value;
            baseQuery = baseQuery.Where(p => (p.DiscountPrice ?? p.Price) <= max);
        }

        foreach (var (attrId, optionIds) in filter.Attributes.Where(kv => kv.Value.Any()))
        {
            var aid = attrId;
            var opts = optionIds;
            baseQuery = baseQuery.Where(p =>
                p.Variants.Any(v => v.IsActive &&
                    v.AttributeValues.Any(av => av.AttributeId == aid && opts.Contains(av.OptionId)))
                ||
                p.AttributeValues.Any(av => av.AttributeId == aid && opts.Contains(av.OptionId)));
        }

        var total = await baseQuery.CountAsync();
        var sortedQuery = ApplySorting(baseQuery, filter.SortBy);

        var page = Math.Max(1, filter.Page);
        var products = await sortedQuery
            .Skip((page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => new ProductCardDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                          ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault(),
                HoverImageUrl = p.Images.Where(i => !i.IsMain)
                    .OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault(),
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                DiscountPercent = p.DiscountPrice.HasValue && p.Price > 0
                    ? (int)Math.Round((p.Price - p.DiscountPrice.Value) / p.Price * 100) : 0,
                Rating = p.Reviews.Any() ? p.Reviews.Average(r => (double?)r.Rating) : null,
                ReviewCount = p.Reviews.Count,
                IsNew = p.CreatedAt >= DateTime.UtcNow.AddDays(-14),
                Stock = p.HasVariants ? p.Variants.Where(v => v.IsActive).Sum(v => v.Stock) : p.Stock,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                CategoryName = p.Category.Name,
                ViewCount = p.ViewCount
            })
            .ToListAsync();

        var (catFacets, brandFacets, priceRange, attrFacets) = await BuildFacetsAsync(filter);

        string? pageTitle = null, pageDescription = null;
        if (filter.Categories.Count == 1)
        {
            var cat = await _db.Categories
                .FirstOrDefaultAsync(c => c.Slug == filter.Categories[0]);
            if (cat != null)
            {
                pageTitle = cat.MetaTitle ?? $"خرید محصولات {cat.Name}";
                pageDescription = cat.MetaDescription
                    ?? $"لیست محصولات دسته‌بندی {cat.Name} با بهترین قیمت";
            }
        }
        else if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            pageTitle = $"نتایج جستجو: {filter.Search}";
        }

        return new ShopResultDto
        {
            Products = products,
            Total = total,
            Page = page,
            PageSize = filter.PageSize,
            Filter = filter,
            Categories = catFacets,
            Brands = brandFacets,
            PriceRange = priceRange,
            AttributeFacets = attrFacets,
            PageTitle = pageTitle,
            PageDescription = pageDescription
        };
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> q, ShopSortBy sort)
        => sort switch
        {
            ShopSortBy.Newest => q.OrderByDescending(p => p.CreatedAt),
            ShopSortBy.Oldest => q.OrderBy(p => p.CreatedAt),
            ShopSortBy.PriceAsc => q.OrderBy(p => p.DiscountPrice ?? p.Price),
            ShopSortBy.PriceDesc => q.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            ShopSortBy.BestSelling => q.OrderByDescending(p =>
                p.OrderItems.Sum(oi => (int?)oi.Quantity) ?? 0),
            ShopSortBy.TopRated => q.OrderByDescending(p =>
                p.Reviews.Any() ? p.Reviews.Average(r => (double?)r.Rating) : 0),
            ShopSortBy.MostDiscounted => q.Where(p => p.DiscountPrice != null)
                .OrderByDescending(p => (p.Price - p.DiscountPrice!.Value) / p.Price),
            ShopSortBy.MostViewed => q.OrderByDescending(p => p.ViewCount),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

    private async Task<List<int>> GetCategoryIdsWithDescendantsAsync(List<string> slugs)
    {
        var roots = await _db.Categories.Where(c => slugs.Contains(c.Slug))
            .Select(c => c.Id).ToListAsync();
        if (!roots.Any()) return new List<int>();

        var allIds = new HashSet<int>(roots);
        var currentLevel = roots;

        for (int depth = 0; depth < 3; depth++)
        {
            var children = await _db.Categories
                .Where(c => c.ParentId.HasValue && currentLevel.Contains(c.ParentId.Value))
                .Select(c => c.Id).ToListAsync();
            if (!children.Any()) break;
            currentLevel = children.Where(id => allIds.Add(id)).ToList();
            if (!currentLevel.Any()) break;
        }

        return allIds.ToList();
    }

    private async Task<(List<CategoryFacetDto>, List<BrandFacetDto>,
                        PriceRangeDto, List<AttributeFacetGroupDto>)>
        BuildFacetsAsync(ShopFilterDto filter)
    {
        var catQuery = BuildFacetQuery(filter, excludeCategory: true, excludeBrand: false);
        var catCounts = await catQuery.GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() }).ToListAsync();

        var catIds = catCounts.Select(c => c.CategoryId).ToList();
        var cats = await _db.Categories
            .Where(c => catIds.Contains(c.Id) || filter.Categories.Contains(c.Slug))
            .OrderBy(c => c.ParentId == null ? 0 : 1).ThenBy(c => c.DisplayOrder)
            .Select(c => new { c.Id, c.Name, c.Slug, c.ParentId }).ToListAsync();

        var parentIds = cats.Where(c => c.ParentId.HasValue)
            .Select(c => c.ParentId!.Value).Distinct().ToList();
        if (parentIds.Any())
        {
            var parents = await _db.Categories.Where(c => parentIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name, c.Slug, c.ParentId }).ToListAsync();
            foreach (var p in parents)
                if (!cats.Any(c => c.Id == p.Id)) cats.Add(p);
        }

        var catFacets = cats.Select(c => new CategoryFacetDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            ParentId = c.ParentId,
            ProductCount = catCounts.FirstOrDefault(cc => cc.CategoryId == c.Id)?.Count ?? 0,
            IsSelected = filter.Categories.Contains(c.Slug),
            Level = c.ParentId.HasValue ? 1 : 0
        }).Where(c => c.ProductCount > 0 || c.IsSelected)
        .OrderBy(c => c.Level).ThenBy(c => c.Name).ToList();

        var brandQuery = BuildFacetQuery(filter, excludeCategory: false, excludeBrand: true);
        var brandCounts = await brandQuery.Where(p => p.BrandId.HasValue)
            .GroupBy(p => p.BrandId!.Value)
            .Select(g => new { BrandId = g.Key, Count = g.Count() }).ToListAsync();

        var brandIds = brandCounts.Select(b => b.BrandId).ToList();
        var brands = await _db.Brands
            .Where(b => brandIds.Contains(b.Id) || filter.Brands.Contains(b.Slug))
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new { b.Id, b.Name, b.Slug, b.LogoUrl }).ToListAsync();

        var brandFacets = brands.Select(b => new BrandFacetDto
        {
            Id = b.Id,
            Name = b.Name,
            Slug = b.Slug,
            LogoUrl = b.LogoUrl,
            ProductCount = brandCounts.FirstOrDefault(bc => bc.BrandId == b.Id)?.Count ?? 0,
            IsSelected = filter.Brands.Contains(b.Slug)
        }).Where(b => b.ProductCount > 0 || b.IsSelected)
        .OrderByDescending(b => b.ProductCount).ToList();

        var priceQuery = BuildFacetQuery(filter, excludeCategory: false, excludeBrand: false);
        var minPrice = await priceQuery.MinAsync(p => (decimal?)(p.DiscountPrice ?? p.Price)) ?? 0;
        var maxPrice = await priceQuery.MaxAsync(p => (decimal?)(p.DiscountPrice ?? p.Price)) ?? 0;

        var priceRange = new PriceRangeDto
        {
            MinAvailable = minPrice,
            MaxAvailable = maxPrice,
            MinSelected = filter.MinPrice,
            MaxSelected = filter.MaxPrice
        };

        var attrQuery = BuildFacetQuery(filter, excludeCategory: false, excludeBrand: false);
        var productIds = await attrQuery.Select(p => p.Id).ToListAsync();

        var applicableAttributes = await _db.ProductAttributes
            .Where(a => a.IsFilterable).OrderBy(a => a.DisplayOrder).ToListAsync();

        var attrFacets = new List<AttributeFacetGroupDto>();
        foreach (var attr in applicableAttributes)
        {
            var variantCounts = await _db.ProductVariantAttributeValues
                .Where(vav => vav.AttributeId == attr.Id && productIds.Contains(vav.Variant.ProductId))
                .GroupBy(vav => vav.OptionId)
                .Select(g => new { OptionId = g.Key, Count = g.Count() }).ToListAsync();

            var productCounts = await _db.ProductAttributeValues
                .Where(pav => pav.AttributeId == attr.Id && productIds.Contains(pav.ProductId))
                .GroupBy(pav => pav.OptionId)
                .Select(g => new { OptionId = g.Key, Count = g.Count() }).ToListAsync();

            var combinedCounts = variantCounts.Concat(productCounts)
                .GroupBy(x => x.OptionId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

            if (!combinedCounts.Any()) continue;

            var optionIds = combinedCounts.Keys.ToList();
            var options = await _db.ProductAttributeOptions
                .Where(o => optionIds.Contains(o.Id))
                .OrderBy(o => o.DisplayOrder).ToListAsync();

            var selectedOptionIds = filter.Attributes.GetValueOrDefault(attr.Id) ?? new();

            var optionFacets = options.Select(o => new AttributeOptionFacetDto
            {
                OptionId = o.Id,
                Value = o.Value,
                ColorCode = o.ColorCode,
                ProductCount = combinedCounts.GetValueOrDefault(o.Id, 0),
                IsSelected = selectedOptionIds.Contains(o.Id)
            }).ToList();

            if (optionFacets.Any())
            {
                attrFacets.Add(new AttributeFacetGroupDto
                {
                    AttributeId = attr.Id,
                    Name = attr.Name,
                    Slug = attr.Slug,
                    Type = attr.Type,
                    Options = optionFacets
                });
            }
        }

        return (catFacets, brandFacets, priceRange, attrFacets);
    }

    private IQueryable<Product> BuildFacetQuery(ShopFilterDto filter,
        bool excludeCategory, bool excludeBrand)
    {
        var q = _db.Products.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = q.Where(p => p.Name.Contains(s) || (p.Sku != null && p.Sku.Contains(s)));
        }
        if (filter.OnlyInStock == true)
            q = q.Where(p => p.HasVariants
                ? p.Variants.Any(v => v.IsActive && v.Stock > 0) : p.Stock > 0);
        if (filter.OnlyDiscounted == true)
            q = q.Where(p => p.DiscountPrice != null);

        if (!excludeCategory && filter.Categories.Any())
        {
            var ids = _db.Categories
                .Where(c => filter.Categories.Contains(c.Slug) ||
                    (c.Parent != null && filter.Categories.Contains(c.Parent.Slug)))
                .Select(c => c.Id).ToList();
            if (ids.Any()) q = q.Where(p => ids.Contains(p.CategoryId));
        }

        if (!excludeBrand && filter.Brands.Any())
            q = q.Where(p => p.Brand != null && filter.Brands.Contains(p.Brand.Slug));

        if (filter.MinPrice.HasValue)
        {
            var min = filter.MinPrice.Value;
            q = q.Where(p => (p.DiscountPrice ?? p.Price) >= min);
        }
        if (filter.MaxPrice.HasValue)
        {
            var max = filter.MaxPrice.Value;
            q = q.Where(p => (p.DiscountPrice ?? p.Price) <= max);
        }

        foreach (var (attrId, optionIds) in filter.Attributes.Where(kv => kv.Value.Any()))
        {
            var aid = attrId;
            var opts = optionIds;
            q = q.Where(p =>
                p.Variants.Any(v => v.IsActive &&
                    v.AttributeValues.Any(av => av.AttributeId == aid && opts.Contains(av.OptionId)))
                ||
                p.AttributeValues.Any(av => av.AttributeId == aid && opts.Contains(av.OptionId)));
        }

        return q;
    }

    public string BuildUrl(ShopFilterDto current, Action<ShopFilterDto> mutate)
    {
        var f = new ShopFilterDto
        {
            Search = current.Search,
            Categories = new List<string>(current.Categories),
            Brands = new List<string>(current.Brands),
            MinPrice = current.MinPrice,
            MaxPrice = current.MaxPrice,
            Attributes = current.Attributes.ToDictionary(kv => kv.Key, kv => new List<int>(kv.Value)),
            OnlyInStock = current.OnlyInStock,
            OnlyDiscounted = current.OnlyDiscounted,
            SortBy = current.SortBy,
            Page = 1,
            PageSize = current.PageSize
        };

        mutate(f);

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(f.Search))
            parts.Add($"search={Uri.EscapeDataString(f.Search)}");
        if (f.Categories.Any())
            parts.Add($"category={string.Join(",", f.Categories)}");
        if (f.Brands.Any())
            parts.Add($"brand={string.Join(",", f.Brands)}");
        if (f.MinPrice.HasValue) parts.Add($"minPrice={f.MinPrice}");
        if (f.MaxPrice.HasValue) parts.Add($"maxPrice={f.MaxPrice}");
        if (f.Attributes.Any(kv => kv.Value.Any()))
        {
            var attrStr = string.Join(",", f.Attributes
                .Where(kv => kv.Value.Any())
                .SelectMany(kv => kv.Value.Select(oid => $"{kv.Key}:{oid}")));
            parts.Add($"attrs={attrStr}");
        }
        if (f.OnlyInStock == true) parts.Add("inStock=true");
        if (f.OnlyDiscounted == true) parts.Add("discounted=true");
        if (f.SortBy != ShopSortBy.Newest) parts.Add($"sort={f.SortBy.ToString().ToLowerInvariant()}");
        if (f.Page > 1) parts.Add($"page={f.Page}");

        var sb = new StringBuilder("/shop");
        if (parts.Any()) sb.Append('?').Append(string.Join("&", parts));
        return sb.ToString();
    }
}