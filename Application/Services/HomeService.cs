using EShop.Application.Data;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using EShop.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EShop.Application.Services;

public class HomeService : IHomeService
{
    private const string CacheKey = "EShop:Home";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IApplicationDbContext _db;
    private readonly ISliderService _sliders;
    private readonly ISettingsService _settings;
    private readonly IMemoryCache _cache;

    public HomeService(IApplicationDbContext db, ISliderService sliders,
                       ISettingsService settings, IMemoryCache cache)
    {
        _db = db; _sliders = sliders; _settings = settings; _cache = cache;
    }

    public Task InvalidateCacheAsync()
    {
        _cache.Remove(CacheKey);
        return Task.CompletedTask;
    }

    public async Task<HomeViewModel> GetHomeDataAsync()
    {
        if (_cache.TryGetValue(CacheKey, out HomeViewModel? cached) && cached != null)
            return cached;

        var vm = new HomeViewModel
        {
            HeroSliders = await _sliders.GetActiveAsync(SliderPosition.HeroMain),
            MiddleBanners = await _sliders.GetActiveAsync(SliderPosition.MiddleBanner),
            FreeShippingThreshold = await _settings.GetDecimalAsync(
                SettingsKeys.ShippingFreeThreshold, 2_000_000m)
        };

        vm.DealsOfTheDay = await BuildProductCardQuery(
            _db.Products.Where(p => p.IsActive && p.IsSpecialOffer && p.DiscountPrice != null)
                .OrderByDescending(p => p.Price - p.DiscountPrice!.Value)
        ).Take(10).ToListAsync();

        vm.TopSelling = await BuildProductCardQuery(
            _db.Products.Where(p => p.IsActive)
                .OrderByDescending(p => p.OrderItems.Sum(oi => (int?)oi.Quantity) ?? 0)
        ).Take(10).ToListAsync();

        vm.Newest = await BuildProductCardQuery(
            _db.Products.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt)
        ).Take(10).ToListAsync();

        vm.Featured = await BuildProductCardQuery(
            _db.Products.Where(p => p.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
        ).Take(10).ToListAsync();

        vm.TopRated = await BuildProductCardQuery(
            _db.Products.Where(p => p.IsActive && p.Reviews.Any())
                .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
        ).Take(10).ToListAsync();

        vm.MostViewed = await BuildProductCardQuery(
            _db.Products.Where(p => p.IsActive && p.ViewCount > 0)
                .OrderByDescending(p => p.ViewCount)
        ).Take(10).ToListAsync();

        vm.CategoryShowcase = await _db.Categories
            .Where(c => c.IsActive && c.ShowInMenu && c.ParentId == null)
            .OrderBy(c => c.DisplayOrder)
            .Take(8)
            .Select(c => new CategoryWithImageDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ImageUrl = c.ImageUrl,
                Icon = c.Icon,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        vm.Brands = await _db.Brands
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .Take(12)
            .Select(b => new BrandCardDto
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                LogoUrl = b.LogoUrl,
                ProductCount = b.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        _cache.Set(CacheKey, vm, CacheDuration);
        return vm;
    }

    private static IQueryable<ProductCardDto> BuildProductCardQuery(IQueryable<Product> query)
    {
        return query.Select(p => new ProductCardDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            ImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                      ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault(),
            HoverImageUrl = p.Images.Where(i => !i.IsMain).OrderBy(i => i.DisplayOrder)
                .Select(i => i.Path).FirstOrDefault(),
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            DiscountPercent = p.DiscountPrice.HasValue && p.Price > 0
                ? (int)Math.Round((p.Price - p.DiscountPrice.Value) / p.Price * 100) : 0,
            Rating = p.Reviews.Any() ? p.Reviews.Average(r => (double?)r.Rating) : null,
            ReviewCount = p.Reviews.Count,
            IsNew = p.CreatedAt >= DateTime.UtcNow.AddDays(-14),
            Stock = p.HasVariants
                ? p.Variants.Where(v => v.IsActive).Sum(v => v.Stock)
                : p.Stock,
            BrandName = p.Brand != null ? p.Brand.Name : null,
            CategoryName = p.Category.Name,
            ViewCount = p.ViewCount
        });
    }
}