using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EShop.Application.DTOs;

public class BrandListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? LogoUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BrandEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام برند الزامی است")]
    [MaxLength(150)]
    [Display(Name = "نام برند")]
    public string Name { get; set; } = "";

    [MaxLength(200)]
    [Display(Name = "نامک (Slug)")]
    public string? Slug { get; set; }

    [MaxLength(1000)]
    [Display(Name = "توضیحات")]
    public string? Description { get; set; }

    [Display(Name = "وب‌سایت")]
    [Url(ErrorMessage = "آدرس وب‌سایت معتبر نیست")]
    [MaxLength(200)]
    public string? Website { get; set; }

    [Display(Name = "ترتیب نمایش")]
    [Range(0, 9999)]
    public int DisplayOrder { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "عنوان سئو")]
    [MaxLength(70)]
    public string? MetaTitle { get; set; }

    [Display(Name = "توضیحات سئو")]
    [MaxLength(160)]
    public string? MetaDescription { get; set; }

    [Display(Name = "لوگو")]
    public IFormFile? LogoFile { get; set; }

    public string? ExistingLogoUrl { get; set; }
    public bool RemoveLogo { get; set; }
}