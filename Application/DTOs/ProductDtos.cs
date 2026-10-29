using EShop.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EShop.Application.DTOs;

// ============ لیست محصولات (پنل ادمین) ============
public class ProductListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? MainImage { get; set; }
    public string CategoryName { get; set; } = "";
    public string? BrandName { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public bool HasVariants { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============ ایجاد/ویرایش محصول ============
public class ProductEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام محصول الزامی است")]
    [MaxLength(250)]
    [Display(Name = "نام محصول")]
    public string Name { get; set; } = "";

    [MaxLength(250)]
    [Display(Name = "نامک (Slug)")]
    public string? Slug { get; set; }

    [MaxLength(100)]
    [Display(Name = "SKU")]
    public string? Sku { get; set; }

    [Required(ErrorMessage = "دسته‌بندی الزامی است")]
    [Display(Name = "دسته‌بندی")]
    public int CategoryId { get; set; }

    [Display(Name = "برند")]
    public int? BrandId { get; set; }

    [Required(ErrorMessage = "قیمت الزامی است")]
    [Range(0, 1_000_000_000, ErrorMessage = "قیمت معتبر نیست")]
    [Display(Name = "قیمت (تومان)")]
    public decimal Price { get; set; }

    [Range(0, 1_000_000_000)]
    [Display(Name = "قیمت با تخفیف")]
    public decimal? DiscountPrice { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "موجودی معتبر نیست")]
    [Display(Name = "موجودی")]
    public int Stock { get; set; }

    [Range(0, 1000)]
    [Display(Name = "آستانه هشدار موجودی")]
    public int? LowStockThreshold { get; set; } = 5;

    [Range(0, 100000)]
    [Display(Name = "وزن (گرم)")]
    public decimal? Weight { get; set; }

    [MaxLength(100)]
    [Display(Name = "ابعاد")]
    public string? Dimensions { get; set; }

    [MaxLength(500)]
    [Display(Name = "توضیح کوتاه")]
    public string? ShortDescription { get; set; }

    [Display(Name = "توضیحات کامل")]
    public string? FullDescription { get; set; }

    [Display(Name = "فعال")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "ویژه")]
    public bool IsFeatured { get; set; }

    [Display(Name = "پیشنهاد ویژه")]
    public bool IsSpecialOffer { get; set; }

    [Display(Name = "دارای متغیرها")]
    public bool HasVariants { get; set; }

    [MaxLength(70)]
    [Display(Name = "عنوان سئو")]
    public string? MetaTitle { get; set; }

    [MaxLength(160)]
    [Display(Name = "توضیحات سئو")]
    public string? MetaDescription { get; set; }

    [MaxLength(300)]
    [Display(Name = "کلمات کلیدی")]
    public string? MetaKeywords { get; set; }

    // تصاویر
    public List<ImageItemDto> Images { get; set; } = new();
    public List<IFormFile>? NewImageFiles { get; set; }

    // ویژگی‌های نمایشی
    public List<ProductAttributeSelectionDto> AttributeSelections { get; set; } = new();

    // متغیرها
    public List<VariantItemDto> Variants { get; set; } = new();
}

public class ImageItemDto
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public string? Alt { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsMain { get; set; }
    public bool ToDelete { get; set; }
}

public class ProductAttributeSelectionDto
{
    public int AttributeId { get; set; }
    public string AttributeName { get; set; } = "";
    public int OptionId { get; set; }
    public string OptionValue { get; set; } = "";
}

public class VariantItemDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public List<VariantAttributeValueDto> Attributes { get; set; } = new();
}

public class VariantAttributeValueDto
{
    public int AttributeId { get; set; }
    public int OptionId { get; set; }
}

// ============ Dropdownهای ویژگی ============
public class AttributeWithOptionsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public AttributeType Type { get; set; }
    public List<AttributeOptionDto> Options { get; set; } = new();
}

public class AttributeOptionDto
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
    public string? ColorCode { get; set; }
}