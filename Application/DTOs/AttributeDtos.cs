using System.ComponentModel.DataAnnotations;
using EShop.Domain.Enums;

namespace EShop.Application.DTOs;

public class AttributeListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public AttributeType Type { get; set; }
    public bool IsFilterable { get; set; }
    public int DisplayOrder { get; set; }
    public int OptionCount { get; set; }
    public int ProductUsageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttributeEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام ویژگی الزامی است")]
    [MaxLength(100)]
    [Display(Name = "نام ویژگی")]
    public string Name { get; set; } = "";

    [MaxLength(100)]
    [Display(Name = "نامک (Slug)")]
    public string? Slug { get; set; }

    [Required]
    [Display(Name = "نوع نمایش")]
    public AttributeType Type { get; set; } = AttributeType.Select;

    [Range(0, 9999)]
    [Display(Name = "ترتیب نمایش")]
    public int DisplayOrder { get; set; }

    [Display(Name = "قابل فیلتر در فروشگاه")]
    public bool IsFilterable { get; set; } = true;

    public List<OptionItemDto> Options { get; set; } = new();
}

public class OptionItemDto
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
    public string? ColorCode { get; set; }
    public int DisplayOrder { get; set; }
    public int UsageCount { get; set; }
}

public class OptionCreateDto
{
    [Required(ErrorMessage = "مقدار الزامی است")]
    [MaxLength(100)]
    public string Value { get; set; } = "";

    [MaxLength(10)]
    public string? ColorCode { get; set; }

    public int DisplayOrder { get; set; }
}

public class OptionUpdateDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "مقدار الزامی است")]
    [MaxLength(100)]
    public string Value { get; set; } = "";

    [MaxLength(10)]
    public string? ColorCode { get; set; }

    public int DisplayOrder { get; set; }
}