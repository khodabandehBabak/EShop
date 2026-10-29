using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EShop.Application.DTOs;

public class CategoryListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Icon { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public bool ShowInMenu { get; set; }
    public int ProductCount { get; set; }
    public int ChildrenCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CategoryEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
    [MaxLength(150, ErrorMessage = "حداکثر ۱۵۰ کاراکتر")]
    [Display(Name = "نام دسته‌بندی")]
    public string Name { get; set; } = "";

    [MaxLength(200)]
    [Display(Name = "نامک (Slug)")]
    public string? Slug { get; set; }

    [MaxLength(500)]
    [Display(Name = "توضیحات")]
    public string? Description { get; set; }

    [Display(Name = "آیکون")]
    [MaxLength(100)]
    public string? Icon { get; set; }

    [Display(Name = "دسته والد")]
    public int? ParentId { get; set; }

    [Display(Name = "ترتیب نمایش")]
    [Range(0, 9999, ErrorMessage = "عدد بین ۰ تا ۹۹۹۹")]
    public int DisplayOrder { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "نمایش در منو")]
    public bool ShowInMenu { get; set; } = true;

    [Display(Name = "عنوان سئو")]
    [MaxLength(70)]
    public string? MetaTitle { get; set; }

    [Display(Name = "توضیحات سئو")]
    [MaxLength(160)]
    public string? MetaDescription { get; set; }

    [Display(Name = "کلمات کلیدی")]
    [MaxLength(300)]
    public string? MetaKeywords { get; set; }

    [Display(Name = "تصویر دسته‌بندی")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImageUrl { get; set; }
    public bool RemoveImage { get; set; }
}

public class CategoryTreeItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
    public int Level { get; set; }
    public List<CategoryTreeItemDto> Children { get; set; } = new();
}