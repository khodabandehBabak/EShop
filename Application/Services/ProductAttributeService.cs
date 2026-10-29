using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class ProductAttributeService : IProductAttributeService
{
    private readonly IApplicationDbContext _db;
    private readonly ISlugService _slugs;

    public ProductAttributeService(IApplicationDbContext db, ISlugService slugs)
    {
        _db = db; _slugs = slugs;
    }

    public async Task<List<AttributeListItemDto>> GetAllAsync(string? search = null)
    {
        var q = _db.ProductAttributes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(a => a.Name.Contains(search) || a.Slug.Contains(search));

        var list = await q
            .OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name)
            .Select(a => new AttributeListItemDto
            {
                Id = a.Id,
                Name = a.Name,
                Slug = a.Slug,
                Type = a.Type,
                IsFilterable = a.IsFilterable,
                DisplayOrder = a.DisplayOrder,
                OptionCount = a.Options.Count,
                ProductUsageCount = _db.ProductAttributeValues
                    .Where(pav => pav.AttributeId == a.Id)
                    .Select(pav => pav.ProductId).Distinct().Count(),
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return list;
    }

    public async Task<AttributeEditDto?> GetForEditAsync(int id)
    {
        var a = await _db.ProductAttributes
            .Include(x => x.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (a == null) return null;

        var optionIds = a.Options.Select(o => o.Id).ToList();
        var usageMap = await _db.ProductVariantAttributeValues
            .Where(v => optionIds.Contains(v.OptionId))
            .GroupBy(v => v.OptionId)
            .Select(g => new { OptionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OptionId, x => x.Count);

        return new AttributeEditDto
        {
            Id = a.Id,
            Name = a.Name,
            Slug = a.Slug,
            Type = a.Type,
            DisplayOrder = a.DisplayOrder,
            IsFilterable = a.IsFilterable,
            Options = a.Options.Select(o => new OptionItemDto
            {
                Id = o.Id,
                Value = o.Value,
                ColorCode = o.ColorCode,
                DisplayOrder = o.DisplayOrder,
                UsageCount = usageMap.GetValueOrDefault(o.Id, 0)
            }).ToList()
        };
    }

    public async Task<int> CreateAsync(AttributeEditDto dto)
    {
        var slug = await _slugs.GenerateUniqueAsync(
            string.IsNullOrWhiteSpace(dto.Slug) ? dto.Name : dto.Slug,
            s => _db.ProductAttributes.AnyAsync(a => a.Slug == s));

        var attr = new ProductAttribute
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Type = dto.Type,
            DisplayOrder = dto.DisplayOrder,
            IsFilterable = dto.IsFilterable,
            CreatedAt = DateTime.UtcNow
        };

        _db.ProductAttributes.Add(attr);
        await _db.SaveChangesAsync();
        return attr.Id;
    }

    public async Task UpdateAsync(AttributeEditDto dto)
    {
        var attr = await _db.ProductAttributes.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("ویژگی یافت نشد");

        if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != attr.Slug)
            attr.Slug = await _slugs.GenerateUniqueAsync(dto.Slug,
                s => _db.ProductAttributes.AnyAsync(a => a.Slug == s && a.Id != dto.Id));

        if (attr.Type != dto.Type)
        {
            var hasUsage = await _db.ProductAttributeValues.AnyAsync(pav => pav.AttributeId == attr.Id)
                || await _db.ProductVariantAttributeValues.AnyAsync(v => v.AttributeId == attr.Id);
            if (hasUsage)
                throw new InvalidOperationException(
                    "این ویژگی در محصولات استفاده شده و نوع آن قابل تغییر نیست");
        }

        attr.Name = dto.Name.Trim();
        attr.Type = dto.Type;
        attr.DisplayOrder = dto.DisplayOrder;
        attr.IsFilterable = dto.IsFilterable;
        attr.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var attr = await _db.ProductAttributes.Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (attr == null) return false;

        if (await _db.ProductAttributeValues.AnyAsync(pav => pav.AttributeId == id))
            throw new InvalidOperationException("این ویژگی به محصولات متصل است");

        if (await _db.ProductVariantAttributeValues.AnyAsync(v => v.AttributeId == id))
            throw new InvalidOperationException("این ویژگی در متغیرها استفاده شده است");

        _db.ProductAttributes.Remove(attr);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleFilterableAsync(int id)
    {
        var attr = await _db.ProductAttributes.FindAsync(id);
        if (attr == null) return false;
        attr.IsFilterable = !attr.IsFilterable;
        attr.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return attr.IsFilterable;
    }

    public async Task<OptionItemDto> AddOptionAsync(int attributeId, OptionCreateDto dto)
    {
        var attr = await _db.ProductAttributes.FindAsync(attributeId)
            ?? throw new InvalidOperationException("ویژگی یافت نشد");

        var value = dto.Value.Trim();
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException("مقدار نمی‌تواند خالی باشد");

        if (await _db.ProductAttributeOptions
            .AnyAsync(o => o.AttributeId == attributeId && o.Value == value))
            throw new InvalidOperationException("این مقدار قبلاً ثبت شده است");

        var colorCode = ValidateColorCode(attr.Type, dto.ColorCode);

        int order = dto.DisplayOrder;
        if (order <= 0)
        {
            order = (await _db.ProductAttributeOptions
                .Where(o => o.AttributeId == attributeId)
                .MaxAsync(o => (int?)o.DisplayOrder) ?? 0) + 1;
        }

        var option = new ProductAttributeOption
        {
            AttributeId = attributeId,
            Value = value,
            ColorCode = colorCode,
            DisplayOrder = order
        };
        _db.ProductAttributeOptions.Add(option);
        await _db.SaveChangesAsync();

        return new OptionItemDto
        {
            Id = option.Id,
            Value = option.Value,
            ColorCode = option.ColorCode,
            DisplayOrder = option.DisplayOrder
        };
    }

    public async Task<OptionItemDto> UpdateOptionAsync(int attributeId, OptionUpdateDto dto)
    {
        var attr = await _db.ProductAttributes.FindAsync(attributeId)
            ?? throw new InvalidOperationException("ویژگی یافت نشد");

        var option = await _db.ProductAttributeOptions
            .FirstOrDefaultAsync(o => o.Id == dto.Id && o.AttributeId == attributeId)
            ?? throw new InvalidOperationException("مقدار یافت نشد");

        var value = dto.Value.Trim();
        if (string.IsNullOrEmpty(value))
            throw new InvalidOperationException("مقدار نمی‌تواند خالی باشد");

        if (await _db.ProductAttributeOptions
            .AnyAsync(o => o.AttributeId == attributeId && o.Value == value && o.Id != dto.Id))
            throw new InvalidOperationException("این مقدار قبلاً ثبت شده است");

        option.Value = value;
        option.ColorCode = ValidateColorCode(attr.Type, dto.ColorCode);
        option.DisplayOrder = dto.DisplayOrder;

        await _db.SaveChangesAsync();

        return new OptionItemDto
        {
            Id = option.Id,
            Value = option.Value,
            ColorCode = option.ColorCode,
            DisplayOrder = option.DisplayOrder
        };
    }

    public async Task<bool> DeleteOptionAsync(int attributeId, int optionId)
    {
        var option = await _db.ProductAttributeOptions
            .FirstOrDefaultAsync(o => o.Id == optionId && o.AttributeId == attributeId);
        if (option == null) return false;

        if (await _db.ProductVariantAttributeValues.AnyAsync(v => v.OptionId == optionId))
            throw new InvalidOperationException("این مقدار در متغیرها استفاده شده است");

        if (await _db.ProductAttributeValues.AnyAsync(pav => pav.OptionId == optionId))
            throw new InvalidOperationException("این مقدار در محصولات استفاده شده است");

        _db.ProductAttributeOptions.Remove(option);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderOptionsAsync(int attributeId, List<int> orderedOptionIds)
    {
        var options = await _db.ProductAttributeOptions
            .Where(o => o.AttributeId == attributeId && orderedOptionIds.Contains(o.Id))
            .ToListAsync();

        for (int i = 0; i < orderedOptionIds.Count; i++)
        {
            var opt = options.FirstOrDefault(o => o.Id == orderedOptionIds[i]);
            if (opt != null) opt.DisplayOrder = i + 1;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    private static string? ValidateColorCode(AttributeType type, string? colorCode)
    {
        if (type != AttributeType.Color) return null;

        if (string.IsNullOrWhiteSpace(colorCode))
            throw new InvalidOperationException("برای ویژگی رنگی، کد رنگ الزامی است");

        colorCode = colorCode.Trim();
        if (!colorCode.StartsWith("#")) colorCode = "#" + colorCode;

        if (!System.Text.RegularExpressions.Regex.IsMatch(colorCode, "^#[0-9A-Fa-f]{6}$"))
            throw new InvalidOperationException("کد رنگ باید به فرمت #RRGGBB باشد");

        return colorCode.ToLowerInvariant();
    }
}