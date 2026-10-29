using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class ProductStorefrontService : IProductStorefrontService
{
    private readonly IApplicationDbContext _db;

    public ProductStorefrontService(IApplicationDbContext db) => _db = db;

    public async Task<ProductDetailDto?> GetBySlugAsync(string slug)
    {
        var p = await _db.Products
            .Include(x => x.Images)
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Reviews)
            .Include(x => x.AttributeValues).ThenInclude(av => av.Attribute)
            .Include(x => x.AttributeValues).ThenInclude(av => av.Option)
            .Include(x => x.Variants).ThenInclude(v => v.AttributeValues)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive);

        if (p == null) return null;

        var totalStock = p.HasVariants
            ? p.Variants.Where(v => v.IsActive).Sum(v => v.Stock)
            : p.Stock;

        var discountPercent = p.DiscountPrice.HasValue && p.Price > 0
            ? (int)Math.Round((p.Price - p.DiscountPrice.Value) / p.Price * 100) : 0;

        var groups = new List<VariantAttributeGroupDto>();
        var activeVariants = p.Variants.Where(v => v.IsActive).ToList();

        if (p.HasVariants && activeVariants.Any())
        {
            var attrIds = activeVariants
                .SelectMany(v => v.AttributeValues.Select(av => av.AttributeId))
                .Distinct().ToList();

            var attributes = await _db.ProductAttributes
                .Where(a => attrIds.Contains(a.Id))
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();

            foreach (var attr in attributes)
            {
                var optionIds = activeVariants
                    .SelectMany(v => v.AttributeValues
                        .Where(av => av.AttributeId == attr.Id)
                        .Select(av => av.OptionId))
                    .Distinct().ToList();

                var options = await _db.ProductAttributeOptions
                    .Where(o => o.AttributeId == attr.Id && optionIds.Contains(o.Id))
                    .OrderBy(o => o.DisplayOrder)
                    .ToListAsync();

                groups.Add(new VariantAttributeGroupDto
                {
                    AttributeId = attr.Id,
                    Name = attr.Name,
                    Slug = attr.Slug,
                    Type = attr.Type,
                    Options = options.Select(o => new VariantAttributeOptionDto
                    {
                        OptionId = o.Id,
                        Value = o.Value,
                        ColorCode = o.ColorCode,
                        IsAvailable = true
                    }).ToList()
                });
            }
        }

        var variants = activeVariants.Select(v => new VariantDto
        {
            Id = v.Id,
            Sku = v.Sku,
            Price = v.Price,
            DiscountPrice = v.DiscountPrice,
            Stock = v.Stock,
            ImageUrl = v.ImageUrl,
            AttributeValues = v.AttributeValues.ToDictionary(av => av.AttributeId, av => av.OptionId)
        }).ToList();

        var reviews = p.Reviews.ToList();
        var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : (double?)null;

        var specs = p.AttributeValues
            .OrderBy(av => av.Attribute.DisplayOrder)
            .Select(av => new AttributeSpecDto
            {
                Name = av.Attribute.Name,
                Value = av.Option.Value,
                ColorCode = av.Option.ColorCode
            }).ToList();

        return new ProductDetailDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Sku = p.Sku,
            ShortDescription = p.ShortDescription,
            FullDescription = p.FullDescription,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            DiscountPercent = discountPercent,
            Stock = totalStock,
            HasVariants = p.HasVariants,
            Weight = p.Weight,
            Dimensions = p.Dimensions,
            IsInStock = totalStock > 0,
            IsLowStock = totalStock > 0 && totalStock <= (p.LowStockThreshold ?? 5),
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            CategorySlug = p.Category.Slug,
            BrandName = p.Brand?.Name,
            BrandSlug = p.Brand?.Slug,
            BrandLogoUrl = p.Brand?.LogoUrl,
            AvgRating = avgRating,
            ReviewCount = reviews.Count,
            Rating5Count = reviews.Count(r => r.Rating == 5),
            Rating4Count = reviews.Count(r => r.Rating == 4),
            Rating3Count = reviews.Count(r => r.Rating == 3),
            Rating2Count = reviews.Count(r => r.Rating == 2),
            Rating1Count = reviews.Count(r => r.Rating == 1),
            Images = p.Images.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    Path = i.Path,
                    Alt = i.Alt,
                    IsMain = i.IsMain
                }).ToList(),
            Specs = specs,
            AttributeGroups = groups,
            Variants = variants,
            MetaTitle = p.MetaTitle,
            MetaDescription = p.MetaDescription,
            MetaKeywords = p.MetaKeywords
        };
    }

    public async Task<List<ProductCardDto>> GetRelatedAsync(int productId, int categoryId, int take = 8)
    {
        return await _db.Products
            .Where(x => x.IsActive && x.Id != productId && x.CategoryId == categoryId)
            .OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new ProductCardDto
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                ImageUrl = x.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                          ?? x.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault(),
                HoverImageUrl = x.Images.Where(i => !i.IsMain).OrderBy(i => i.DisplayOrder)
                    .Select(i => i.Path).FirstOrDefault(),
                Price = x.Price,
                DiscountPrice = x.DiscountPrice,
                DiscountPercent = x.DiscountPrice.HasValue && x.Price > 0
                    ? (int)Math.Round((x.Price - x.DiscountPrice.Value) / x.Price * 100) : 0,
                Rating = x.Reviews.Any() ? x.Reviews.Average(r => (double?)r.Rating) : null,
                ReviewCount = x.Reviews.Count,
                IsNew = x.CreatedAt >= DateTime.UtcNow.AddDays(-14),
                Stock = x.HasVariants ? x.Variants.Where(v => v.IsActive).Sum(v => v.Stock) : x.Stock,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                CategoryName = x.Category.Name,
                ViewCount = x.ViewCount
            })
            .ToListAsync();
    }

    public Task IncrementViewAsync(int productId)
        => Task.CompletedTask;
}