using EShop.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EShop.Application.DTOs;

public class SliderListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string ImageUrl { get; set; } = "";
    public SliderPosition Position { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SliderEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الزامی است")]
    [MaxLength(200)]
    [Display(Name = "عنوان")]
    public string Title { get; set; } = "";

    [MaxLength(300)]
    [Display(Name = "زیرعنوان")]
    public string? Subtitle { get; set; }

    [MaxLength(500)]
    [Display(Name = "توضیحات")]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "موقعیت نمایش")]
    public SliderPosition Position { get; set; } = SliderPosition.HeroMain;

    [Range(0, 9999)]
    [Display(Name = "ترتیب نمایش")]
    public int DisplayOrder { get; set; } = 100;

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    [MaxLength(50)]
    [Display(Name = "متن دکمه")]
    public string? BtnText { get; set; }

    [MaxLength(500)]
    [Display(Name = "لینک دکمه")]
    public string? BtnLink { get; set; }

    [MaxLength(20)]
    [Display(Name = "رنگ پس‌زمینه")]
    public string? BgColor { get; set; } = "#f1f0f0";

    [MaxLength(20)]
    [Display(Name = "رنگ متن")]
    public string? TextColor { get; set; } = "#333";

    [Display(Name = "شروع نمایش")]
    public DateTime? StartDate { get; set; }

    [Display(Name = "پایان نمایش")]
    public DateTime? EndDate { get; set; }

    [MaxLength(200)]
    [Display(Name = "Alt (سئو)")]
    public string? Alt { get; set; }

    [Display(Name = "تصویر اصلی")]
    public IFormFile? ImageFile { get; set; }
    public string? ExistingImageUrl { get; set; }

    [Display(Name = "تصویر محصول شناور")]
    public IFormFile? ProductImageFile { get; set; }
    public string? ExistingProductImageUrl { get; set; }

    [Display(Name = "تصویر موبایل")]
    public IFormFile? ImageMobileFile { get; set; }
    public string? ExistingMobileImageUrl { get; set; }
}

public class SliderPublicDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string ImageUrl { get; set; } = "";
    public string? ImageMobileUrl { get; set; }
    public string? ProductImageUrl { get; set; }
    public string? BtnText { get; set; }
    public string? BtnLink { get; set; }
    public string? BgColor { get; set; }
    public string? TextColor { get; set; }
    public string? Alt { get; set; }
}