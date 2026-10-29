using EShop.Application.Data;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class SliderService : ISliderService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileService _files;

    public SliderService(IApplicationDbContext db, IFileService files)
    {
        _db = db; _files = files;
    }

    public async Task<List<SliderListItemDto>> GetAllAsync(SliderPosition? position = null)
    {
        var q = _db.Sliders.AsQueryable();
        if (position.HasValue) q = q.Where(s => s.Position == position);

        return await q.OrderBy(s => s.Position).ThenBy(s => s.DisplayOrder)
            .Select(s => new SliderListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                Subtitle = s.Subtitle,
                ImageUrl = s.ImageUrl,
                Position = s.Position,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                CreatedAt = s.CreatedAt
            }).ToListAsync();
    }

    public async Task<SliderEditDto?> GetForEditAsync(int id)
    {
        var s = await _db.Sliders.FindAsync(id);
        if (s == null) return null;

        return new SliderEditDto
        {
            Id = s.Id,
            Title = s.Title,
            Subtitle = s.Subtitle,
            Description = s.Description,
            Position = s.Position,
            DisplayOrder = s.DisplayOrder,
            IsActive = s.IsActive,
            BtnText = s.BtnText,
            BtnLink = s.BtnLink,
            BgColor = s.BgColor,
            TextColor = s.TextColor,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Alt = s.Alt,
            ExistingImageUrl = s.ImageUrl,
            ExistingProductImageUrl = s.ProductImageUrl,
            ExistingMobileImageUrl = s.ImageMobileUrl
        };
    }

    public async Task<int> CreateAsync(SliderEditDto dto)
    {
        if (dto.ImageFile == null)
            throw new InvalidOperationException("تصویر اصلی الزامی است");

        var imageUrl = await _files.SaveImageAsync(dto.ImageFile, "sliders", 1920);
        string? productImageUrl = dto.ProductImageFile != null
            ? await _files.SaveImageAsync(dto.ProductImageFile, "sliders", 800) : null;
        string? mobileImageUrl = dto.ImageMobileFile != null
            ? await _files.SaveImageAsync(dto.ImageMobileFile, "sliders", 800) : null;

        var slider = new Slider
        {
            Title = dto.Title.Trim(),
            Subtitle = dto.Subtitle,
            Description = dto.Description,
            ImageUrl = imageUrl,
            ProductImageUrl = productImageUrl,
            ImageMobileUrl = mobileImageUrl,
            Position = dto.Position,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            BtnText = dto.BtnText,
            BtnLink = dto.BtnLink,
            BgColor = dto.BgColor,
            TextColor = dto.TextColor,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Alt = dto.Alt,
            CreatedAt = DateTime.UtcNow
        };

        _db.Sliders.Add(slider);
        await _db.SaveChangesAsync();
        return slider.Id;
    }

    public async Task UpdateAsync(SliderEditDto dto)
    {
        var slider = await _db.Sliders.FindAsync(dto.Id)
            ?? throw new InvalidOperationException("اسلایدر یافت نشد");

        if (dto.ImageFile != null)
        {
            await _files.DeleteAsync(slider.ImageUrl);
            slider.ImageUrl = await _files.SaveImageAsync(dto.ImageFile, "sliders", 1920);
        }
        if (dto.ProductImageFile != null)
        {
            await _files.DeleteAsync(slider.ProductImageUrl);
            slider.ProductImageUrl = await _files.SaveImageAsync(dto.ProductImageFile, "sliders", 800);
        }
        if (dto.ImageMobileFile != null)
        {
            await _files.DeleteAsync(slider.ImageMobileUrl);
            slider.ImageMobileUrl = await _files.SaveImageAsync(dto.ImageMobileFile, "sliders", 800);
        }

        slider.Title = dto.Title.Trim();
        slider.Subtitle = dto.Subtitle;
        slider.Description = dto.Description;
        slider.Position = dto.Position;
        slider.DisplayOrder = dto.DisplayOrder;
        slider.IsActive = dto.IsActive;
        slider.BtnText = dto.BtnText;
        slider.BtnLink = dto.BtnLink;
        slider.BgColor = dto.BgColor;
        slider.TextColor = dto.TextColor;
        slider.StartDate = dto.StartDate;
        slider.EndDate = dto.EndDate;
        slider.Alt = dto.Alt;
        slider.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var slider = await _db.Sliders.FindAsync(id);
        if (slider == null) return false;

        await _files.DeleteAsync(slider.ImageUrl);
        await _files.DeleteAsync(slider.ProductImageUrl);
        await _files.DeleteAsync(slider.ImageMobileUrl);

        _db.Sliders.Remove(slider);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var s = await _db.Sliders.FindAsync(id);
        if (s == null) return false;
        s.IsActive = !s.IsActive;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return s.IsActive;
    }

    public async Task<List<SliderPublicDto>> GetActiveAsync(SliderPosition position, int take = 5)
    {
        var now = DateTime.UtcNow;
        return await _db.Sliders
            .Where(s => s.IsActive && s.Position == position)
            .Where(s => !s.StartDate.HasValue || s.StartDate <= now)
            .Where(s => !s.EndDate.HasValue || s.EndDate >= now)
            .OrderBy(s => s.DisplayOrder)
            .Take(take)
            .Select(s => new SliderPublicDto
            {
                Id = s.Id,
                Title = s.Title,
                Subtitle = s.Subtitle,
                Description = s.Description,
                ImageUrl = s.ImageUrl,
                ImageMobileUrl = s.ImageMobileUrl,
                ProductImageUrl = s.ProductImageUrl,
                BtnText = s.BtnText,
                BtnLink = s.BtnLink,
                BgColor = s.BgColor,
                TextColor = s.TextColor,
                Alt = s.Alt
            })
            .ToListAsync();
    }
}