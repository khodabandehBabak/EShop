using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileService _files;
    private readonly ISlugService _slugs;

    public ProductService(IApplicationDbContext db, IFileService files, ISlugService slugs)
    {
        _db = db; _files = files; _slugs = slugs;
    }

    public async Task<(List<ProductListItemDto> Items, int Total)> GetAllAsync(
        string? search, int? categoryId, int? brandId,
        bool? isActive, int page, int pageSize)
    {
        var q = _db.Products.Include(p => p.Category)
            .Include(p => p.Brand).Include(p => p.Images).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || (p.Sku != null && p.Sku.Contains(search)));

        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId.Value);
        if (brandId.HasValue) q = q.Where(p => p.BrandId == brandId.Value);
        if (isActive.HasValue) q = q.Where(p => p.IsActive == isActive.Value);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                MainImage = p.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                          ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault(),
                CategoryName = p.Category.Name,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                Stock = p.Stock,
                HasVariants = p.HasVariants,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                ViewCount = p.ViewCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<ProductEditDto?> GetForEditAsync(int id)
    {
        var p = await _db.Products
            .Include(x => x.Images)
            .Include(x => x.Variants).ThenInclude(v => v.AttributeValues)
            .Include(x => x.AttributeValues)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return null;

        return new ProductEditDto
        {
            Id = p.Id,
            Name = p.Name,
            Slug = p.Slug,
            Sku = p.Sku,
            CategoryId = p.CategoryId,
            BrandId = p.BrandId,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            Stock = p.Stock,
            LowStockThreshold = p.LowStockThreshold,
            Weight = p.Weight,
            Dimensions = p.Dimensions,
            ShortDescription = p.ShortDescription,
            FullDescription = p.FullDescription,
            IsActive = p.IsActive,
            IsFeatured = p.IsFeatured,
            IsSpecialOffer = p.IsSpecialOffer,
            HasVariants = p.HasVariants,
            MetaTitle = p.MetaTitle,
            MetaDescription = p.MetaDescription,
            MetaKeywords = p.MetaKeywords,
            Images = p.Images.OrderBy(i => i.DisplayOrder).Select(i => new ImageItemDto
            {
                Id = i.Id,
                Path = i.Path,
                Alt = i.Alt,
                DisplayOrder = i.DisplayOrder,
                IsMain = i.IsMain
            }).ToList(),
            AttributeSelections = p.AttributeValues.Select(av => new ProductAttributeSelectionDto
            {
                AttributeId = av.AttributeId,
                OptionId = av.OptionId,
                OptionValue = av.Option?.Value ?? ""
            }).ToList(),
            Variants = p.Variants.Select(v => new VariantItemDto
            {
                Id = v.Id,
                Sku = v.Sku,
                Price = v.Price,
                DiscountPrice = v.DiscountPrice,
                Stock = v.Stock,
                IsActive = v.IsActive,
                ImageUrl = v.ImageUrl,
                Attributes = v.AttributeValues.Select(av => new VariantAttributeValueDto
                {
                    AttributeId = av.AttributeId,
                    OptionId = av.OptionId
                }).ToList()
            }).ToList()
        };
    }

    public async Task<int> CreateAsync(ProductEditDto dto)
    {
        var slug = await _slugs.GenerateUniqueAsync(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug,
            s => _db.Products.AnyAsync(p => p.Slug == s));

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Sku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim(),
            CategoryId = dto.CategoryId,
            BrandId = dto.BrandId,
            Price = dto.Price,
            DiscountPrice = dto.DiscountPrice,
            Stock = dto.HasVariants ? 0 : dto.Stock,
            LowStockThreshold = dto.LowStockThreshold,
            Weight = dto.Weight,
            Dimensions = dto.Dimensions,
            ShortDescription = dto.ShortDescription,
            FullDescription = dto.FullDescription,
            IsActive = dto.IsActive,
            IsFeatured = dto.IsFeatured,
            IsSpecialOffer = dto.IsSpecialOffer,
            HasVariants = dto.HasVariants,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            MetaKeywords = dto.MetaKeywords,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await SaveAttributeSelectionsAsync(product.Id, dto.AttributeSelections);
        await SaveImagesAsync(product.Id, dto, null);
        if (dto.HasVariants) await SaveVariantsAsync(product.Id, dto.Variants);

        await _db.SaveChangesAsync();
        return product.Id;
    }

    public async Task UpdateAsync(ProductEditDto dto)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.AttributeValues)
            .Include(p => p.AttributeValues)
            .FirstOrDefaultAsync(p => p.Id == dto.Id)
            ?? throw new InvalidOperationException("محصول یافت نشد");

        if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != product.Slug)
            product.Slug = await _slugs.GenerateUniqueAsync(dto.Slug,
                s => _db.Products.AnyAsync(p => p.Slug == s && p.Id != dto.Id));

        product.Name = dto.Name.Trim();
        product.Sku = string.IsNullOrWhiteSpace(dto.Sku) ? null : dto.Sku.Trim();
        product.CategoryId = dto.CategoryId;
        product.BrandId = dto.BrandId;
        product.Price = dto.Price;
        product.DiscountPrice = dto.DiscountPrice;
        product.Stock = dto.HasVariants ? 0 : dto.Stock;
        product.LowStockThreshold = dto.LowStockThreshold;
        product.Weight = dto.Weight;
        product.Dimensions = dto.Dimensions;
        product.ShortDescription = dto.ShortDescription;
        product.FullDescription = dto.FullDescription;
        product.IsActive = dto.IsActive;
        product.IsFeatured = dto.IsFeatured;
        product.IsSpecialOffer = dto.IsSpecialOffer;
        product.HasVariants = dto.HasVariants;
        product.MetaTitle = dto.MetaTitle;
        product.MetaDescription = dto.MetaDescription;
        product.MetaKeywords = dto.MetaKeywords;
        product.UpdatedAt = DateTime.UtcNow;

        var deletedImages = product.Images
            .Where(i => dto.Images.Any(di => di.Id == i.Id && di.ToDelete)).ToList();
        foreach (var img in deletedImages)
        {
            await _files.DeleteAsync(img.Path);
            _db.ProductImages.Remove(img);
        }

        foreach (var img in product.Images.Where(i => !deletedImages.Contains(i)))
        {
            var upd = dto.Images.FirstOrDefault(di => di.Id == img.Id);
            if (upd != null)
            {
                img.DisplayOrder = upd.DisplayOrder;
                img.IsMain = upd.IsMain;
                img.Alt = upd.Alt;
            }
        }

        _db.ProductAttributeValues.RemoveRange(product.AttributeValues);
        await _db.SaveChangesAsync();
        await SaveAttributeSelectionsAsync(product.Id, dto.AttributeSelections);

        if (dto.HasVariants)
        {
            _db.ProductVariantAttributeValues.RemoveRange(
                product.Variants.SelectMany(v => v.AttributeValues));
            _db.ProductVariants.RemoveRange(product.Variants);
            await _db.SaveChangesAsync();
            await SaveVariantsAsync(product.Id, dto.Variants);
        }
        else if (product.Variants.Any())
        {
            _db.ProductVariantAttributeValues.RemoveRange(
                product.Variants.SelectMany(v => v.AttributeValues));
            _db.ProductVariants.RemoveRange(product.Variants);
        }

        await SaveImagesAsync(product.Id, dto, product);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _db.Products.Include(p => p.Images).Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return false;

        if (await _db.OrderItems.AnyAsync(oi => oi.ProductId == id))
            throw new InvalidOperationException(
                "این محصول در سفارشات ثبت شده و قابل حذف نیست. آن را غیرفعال کنید.");

        foreach (var img in product.Images)
            await _files.DeleteAsync(img.Path);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;
        p.IsActive = !p.IsActive;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return p.IsActive;
    }

    public async Task<bool> ToggleFeaturedAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;
        p.IsFeatured = !p.IsFeatured;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return p.IsFeatured;
    }

    public async Task<List<AttributeWithOptionsDto>> GetAttributesAsync()
    {
        return await _db.ProductAttributes
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .OrderBy(a => a.DisplayOrder)
            .Select(a => new AttributeWithOptionsDto
            {
                Id = a.Id,
                Name = a.Name,
                Slug = a.Slug,
                Type = a.Type,
                Options = a.Options.Select(o => new AttributeOptionDto
                {
                    Id = o.Id,
                    Value = o.Value,
                    ColorCode = o.ColorCode
                }).ToList()
            })
            .ToListAsync();
    }

    private async Task SaveAttributeSelectionsAsync(int productId,
        List<ProductAttributeSelectionDto> selections)
    {
        foreach (var sel in selections.Where(s => s.OptionId > 0))
        {
            _db.ProductAttributeValues.Add(new ProductAttributeValue
            {
                ProductId = productId,
                AttributeId = sel.AttributeId,
                OptionId = sel.OptionId
            });
        }
        await _db.SaveChangesAsync();
    }

    private async Task SaveImagesAsync(int productId, ProductEditDto dto, Product? existing)
    {
        if (dto.NewImageFiles != null && dto.NewImageFiles.Any())
        {
            int order = existing != null && existing.Images.Any()
                ? existing.Images.Max(i => i.DisplayOrder) + 1 : 0;
            bool hasMain = existing?.Images.Any(i => i.IsMain) ?? false;

            foreach (var file in dto.NewImageFiles.Where(f => f != null && f.Length > 0))
            {
                var path = await _files.SaveImageAsync(file, "products", 1200);
                _db.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    Path = path,
                    DisplayOrder = order++,
                    IsMain = !hasMain && (existing?.Images.Any() != true)
                });
                hasMain = true;
            }
            await _db.SaveChangesAsync();
        }

        var mainSelected = dto.Images.FirstOrDefault(i => i.IsMain && i.Id > 0);
        if (mainSelected != null)
        {
            var all = await _db.ProductImages
                .Where(i => i.ProductId == productId).ToListAsync();
            foreach (var img in all)
                img.IsMain = img.Id == mainSelected.Id;
            await _db.SaveChangesAsync();
        }
    }

    private async Task SaveVariantsAsync(int productId, List<VariantItemDto> variants)
    {
        foreach (var v in variants.Where(x => !string.IsNullOrWhiteSpace(x.Sku)))
        {
            string? imageUrl = v.ImageUrl;
            if (v.ImageFile != null)
                imageUrl = await _files.SaveImageAsync(v.ImageFile, "products/variants", 800);

            var variant = new ProductVariant
            {
                ProductId = productId,
                Sku = v.Sku.Trim(),
                Price = v.Price,
                DiscountPrice = v.DiscountPrice,
                Stock = v.Stock,
                IsActive = v.IsActive,
                ImageUrl = imageUrl
            };
            _db.ProductVariants.Add(variant);
            await _db.SaveChangesAsync();

            foreach (var attr in v.Attributes.Where(a => a.OptionId > 0))
            {
                _db.ProductVariantAttributeValues.Add(new ProductVariantAttributeValue
                {
                    VariantId = variant.Id,
                    AttributeId = attr.AttributeId,
                    OptionId = attr.OptionId
                });
            }
        }
        await _db.SaveChangesAsync();
    }
}