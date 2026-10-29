using EShop.Application.Data;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EShop.Application.Services;

public class CategoryService : ICategoryService
{
    private const string MenuCacheKey = "EShop:CategoryMenu";
    private static readonly TimeSpan MenuCacheDuration = TimeSpan.FromMinutes(10);

    private readonly IApplicationDbContext _db;
    private readonly IFileService _files;
    private readonly ISlugService _slugs;
    private readonly IMemoryCache _cache;  

    public CategoryService(IApplicationDbContext db, IFileService files, ISlugService slugs, IMemoryCache cache)
    {
        _db = db; _files = files; _slugs = slugs; _cache = cache;
    }

    public async Task<List<CategoryListItemDto>> GetAllAsync(string? search = null, bool? isActive = null)
    {
        var q = _db.Categories.Include(c => c.Parent).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(c => c.Name.Contains(search));

        if (isActive.HasValue)
            q = q.Where(c => c.IsActive == isActive.Value);

        return await q
            .OrderBy(c => c.ParentId == null ? 0 : 1)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ImageUrl = c.ImageUrl,
                Icon = c.Icon,
                ParentId = c.ParentId,
                ParentName = c.Parent != null ? c.Parent.Name : null,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                ShowInMenu = c.ShowInMenu,
                ProductCount = c.Products.Count,
                ChildrenCount = c.Children.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<CategoryTreeItemDto>> GetTreeAsync(bool onlyActive = false)
    {
        var q = _db.Categories.AsQueryable();
        if (onlyActive) q = q.Where(c => c.IsActive);

        var all = await q
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryTreeItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon,
                Level = 0
            })
            .ToListAsync();

        var byId = all.ToDictionary(x => x.Id);
        var raw = await q.Select(c => new { c.Id, c.ParentId }).ToListAsync();

        foreach (var item in raw)
        {
            if (item.ParentId.HasValue && byId.TryGetValue(item.ParentId.Value, out var parent))
                parent.Children.Add(byId[item.Id]);
        }

        var roots = raw.Where(r => r.ParentId == null)
            .Select(r => byId[r.Id]).ToList();

        void SetLevel(List<CategoryTreeItemDto> nodes, int level)
        {
            foreach (var n in nodes)
            {
                n.Level = level;
                SetLevel(n.Children, level + 1);
            }
        }
        SetLevel(roots, 0);
        return roots;
    }

    public async Task<CategoryEditDto?> GetForEditAsync(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return null;

        return new CategoryEditDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            Icon = c.Icon,
            ParentId = c.ParentId,
            DisplayOrder = c.DisplayOrder,
            IsActive = c.IsActive,
            ShowInMenu = c.ShowInMenu,
            MetaTitle = c.MetaTitle,
            MetaDescription = c.MetaDescription,
            MetaKeywords = c.MetaKeywords,
            ExistingImageUrl = c.ImageUrl
        };
    }

    public async Task<int> CreateAsync(CategoryEditDto dto)
    {
        var slug = await _slugs.GenerateUniqueAsync(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug,
            s => _db.Categories.AnyAsync(c => c.Slug == s));

        if (dto.ParentId.HasValue && !await _db.Categories.AnyAsync(c => c.Id == dto.ParentId))
            throw new InvalidOperationException("دسته والد یافت نشد");

        string? imageUrl = null;
        if (dto.ImageFile != null)
            imageUrl = await _files.SaveImageAsync(dto.ImageFile, "categories", 800);

        var category = new Category
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description,
            Icon = dto.Icon,
            ImageUrl = imageUrl,
            ParentId = dto.ParentId,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            ShowInMenu = dto.ShowInMenu,
            MetaTitle = dto.MetaTitle,
            MetaDescription = dto.MetaDescription,
            MetaKeywords = dto.MetaKeywords,
            CreatedAt = DateTime.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        _cache.Remove(MenuCacheKey);
        return category.Id;
    }

    public async Task UpdateAsync(CategoryEditDto dto)
    {
        var category = await _db.Categories.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("دسته‌بندی یافت نشد");

        if (dto.ParentId == dto.Id)
            throw new InvalidOperationException("نمی‌توانید دسته را والد خودش قرار دهید");

        if (dto.ParentId.HasValue && await IsDescendantAsync(dto.Id, dto.ParentId.Value))
            throw new InvalidOperationException("نمی‌توانید یک زیردسته را والد انتخاب کنید");

        if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != category.Slug)
            category.Slug = await _slugs.GenerateUniqueAsync(dto.Slug,
                s => _db.Categories.AnyAsync(c => c.Slug == s && c.Id != dto.Id));

        if (dto.RemoveImage && !string.IsNullOrWhiteSpace(category.ImageUrl))
        {
            await _files.DeleteAsync(category.ImageUrl);
            category.ImageUrl = null;
        }
        if (dto.ImageFile != null)
        {
            await _files.DeleteAsync(category.ImageUrl);
            category.ImageUrl = await _files.SaveImageAsync(dto.ImageFile, "categories", 800);
        }

        category.Name = dto.Name.Trim();
        category.Description = dto.Description;
        category.Icon = dto.Icon;
        category.ParentId = dto.ParentId;
        category.DisplayOrder = dto.DisplayOrder;
        category.IsActive = dto.IsActive;
        category.ShowInMenu = dto.ShowInMenu;
        category.MetaTitle = dto.MetaTitle;
        category.MetaDescription = dto.MetaDescription;
        category.MetaKeywords = dto.MetaKeywords;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _cache.Remove(MenuCacheKey);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _db.Categories.Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (category == null) return false;

        if (await _db.Products.AnyAsync(p => p.CategoryId == id))
            throw new InvalidOperationException("این دسته دارای محصول است و قابل حذف نیست");

        if (category.Children.Any())
            throw new InvalidOperationException("این دسته دارای زیردسته است. ابتدا زیردسته‌ها را حذف کنید");

        await _files.DeleteAsync(category.ImageUrl);
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        _cache.Remove(MenuCacheKey);
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c == null) return false;
        c.IsActive = !c.IsActive;
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return c.IsActive;
    }

    private async Task<bool> IsDescendantAsync(int ancestorId, int candidateId)
    {
        var current = candidateId;
        for (int depth = 0; depth < 20; depth++)
        {
            var parentId = await _db.Categories
                .Where(c => c.Id == current).Select(c => c.ParentId).FirstOrDefaultAsync();
            if (parentId == null) return false;
            if (parentId == ancestorId) return true;
            current = parentId.Value;
        }
        return false;
    }

    public async Task<List<MegaMenuCategoryDto>> GetMenuAsync()
    {
        if (_cache.TryGetValue(MenuCacheKey, out List<MegaMenuCategoryDto>? cached) && cached != null)
            return cached;

        // خواندن تمام دسته‌بندی‌های فعال با تمام سطوح
        var all = await _db.Categories
            .Where(c => c.IsActive && c.ShowInMenu)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.Icon,
                c.ImageUrl,
                c.ParentId,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        // Level 0 — والدها
        var roots = all.Where(c => c.ParentId == null).ToList();
        var menu = new List<MegaMenuCategoryDto>();

        foreach (var root in roots)
        {
            var dto = new MegaMenuCategoryDto
            {
                Id = root.Id,
                Name = root.Name,
                Slug = root.Slug,
                Icon = root.Icon,
                ImageUrl = root.ImageUrl,
                ProductCount = root.ProductCount
            };

            // Level 1 — فرزندان مستقیم
            var level1 = all.Where(c => c.ParentId == root.Id).ToList();

            foreach (var child in level1)
            {
                var childDto = new MegaMenuChildDto
                {
                    Id = child.Id,
                    Name = child.Name,
                    Slug = child.Slug,
                    ProductCount = child.ProductCount
                };

                // Level 2 — نوه‌ها
                var level2 = all.Where(c => c.ParentId == child.Id).ToList();
                foreach (var grandChild in level2)
                {
                    childDto.Children.Add(new MegaMenuGrandChildDto
                    {
                        Id = grandChild.Id,
                        Name = grandChild.Name,
                        Slug = grandChild.Slug,
                        ProductCount = grandChild.ProductCount
                    });
                }

                dto.Children.Add(childDto);
            }

            // فقط دسته‌هایی که محتوا دارند (محصول یا فرزند) نمایش داده شوند
            if (dto.Children.Any() || dto.ProductCount > 0)
                menu.Add(dto);
        }

        _cache.Set(MenuCacheKey, menu, MenuCacheDuration);
        return menu;
    }
}