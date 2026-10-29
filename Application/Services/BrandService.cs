using EShop.Application.Data;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class BrandService : IBrandService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileService _files;
    private readonly ISlugService _slugs;

    public BrandService(IApplicationDbContext db, IFileService files, ISlugService slugs)
    {
        _db = db; _files = files; _slugs = slugs;
    }

    public async Task<List<BrandListItemDto>> GetAllAsync(string? search = null, bool? isActive = null)
    {
        var q = _db.Brands.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(b => b.Name.Contains(search));

        if (isActive.HasValue)
            q = q.Where(b => b.IsActive == isActive.Value);

        return await q
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name)
            .Select(b => new BrandListItemDto
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                LogoUrl = b.LogoUrl,
                DisplayOrder = b.DisplayOrder,
                IsActive = b.IsActive,
                ProductCount = b.Products.Count,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<BrandEditDto?> GetForEditAsync(int id)
    {
        var b = await _db.Brands.FindAsync(id);
        if (b == null) return null;

        return new BrandEditDto
        {
            Id = b.Id,
            Name = b.Name,
            Slug = b.Slug,
            Description = b.Description,
            Website = b.Website,
            DisplayOrder = b.DisplayOrder,
            IsActive = b.IsActive,
            MetaTitle = b.MetaTitle,
            MetaDescription = b.MetaDescription,
            ExistingLogoUrl = b.LogoUrl
        };
    }

    public async Task<int> CreateAsync(BrandEditDto dto)
    {
        var slug = await _slugs.GenerateUniqueAsync(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug,
            s => _db.Brands.AnyAsync(b => b.Slug == s));

        string? logoUrl = null;
        if (dto.LogoFile != null)
            logoUrl = await _files.SaveImageAsync(dto.LogoFile, "brands", 400);

        var brand = new Brand
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description,
            Website = dto.Website,
            LogoUrl = logoUrl,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            CreatedAt = DateTime.UtcNow
        };

        _db.Brands.Add(brand);
        await _db.SaveChangesAsync();
        return brand.Id;
    }

    public async Task UpdateAsync(BrandEditDto dto)
    {
        var brand = await _db.Brands.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("برند یافت نشد");

        if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != brand.Slug)
            brand.Slug = await _slugs.GenerateUniqueAsync(dto.Slug,
                s => _db.Brands.AnyAsync(b => b.Slug == s && b.Id != dto.Id));

        if (dto.RemoveLogo && !string.IsNullOrWhiteSpace(brand.LogoUrl))
        {
            await _files.DeleteAsync(brand.LogoUrl);
            brand.LogoUrl = null;
        }
        if (dto.LogoFile != null)
        {
            await _files.DeleteAsync(brand.LogoUrl);
            brand.LogoUrl = await _files.SaveImageAsync(dto.LogoFile, "brands", 400);
        }

        brand.Name = dto.Name.Trim();
        brand.Description = dto.Description;
        brand.Website = dto.Website;
        brand.DisplayOrder = dto.DisplayOrder;
        brand.IsActive = dto.IsActive;
        brand.MetaTitle = dto.MetaTitle;
        brand.MetaDescription = dto.MetaDescription;
        brand.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var brand = await _db.Brands.FindAsync(id);
        if (brand == null) return false;

        if (await _db.Products.AnyAsync(p => p.BrandId == id))
            throw new InvalidOperationException("این برند دارای محصول است و قابل حذف نیست");

        await _files.DeleteAsync(brand.LogoUrl);
        _db.Brands.Remove(brand);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var b = await _db.Brands.FindAsync(id);
        if (b == null) return false;
        b.IsActive = !b.IsActive;
        b.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return b.IsActive;
    }
}