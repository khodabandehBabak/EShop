using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace EShop.Application.DTOs;

public class SiteSettingsVm
{
    public GeneralSettingsDto General { get; set; } = new();
    public BrandingSettingsDto Branding { get; set; } = new();
    public ContactSettingsDto Contact { get; set; } = new();
    public SocialSettingsDto Social { get; set; } = new();
    public ShippingSettingsDto Shipping { get; set; } = new();
    public PaymentSettingsDto Payment { get; set; } = new();
    public SeoSettingsDto Seo { get; set; } = new();
}

public class GeneralSettingsDto
{
    [Required(ErrorMessage = "نام سایت الزامی است")]
    [MaxLength(200)]
    public string SiteName { get; set; } = "فروشگاه ستارخان";

    [MaxLength(200)]
    public string? SiteTagline { get; set; }

    [MaxLength(500)]
    public string? SiteDescription { get; set; }

    [MaxLength(500)]
    public string? SiteKeywords { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "تومان";

    [MaxLength(50)]
    public string? TimeZone { get; set; } = "Asia/Tehran";
}

public class BrandingSettingsDto
{
    public IFormFile? LogoFile { get; set; }
    public string? ExistingLogoUrl { get; set; }

    public IFormFile? FooterLogoFile { get; set; }
    public string? ExistingFooterLogoUrl { get; set; }

    public IFormFile? FaviconFile { get; set; }
    public string? ExistingFaviconUrl { get; set; }

    [Range(20, 500)]
    public int? LogoWidth { get; set; } = 144;

    [Range(20, 500)]
    public int? LogoHeight { get; set; } = 45;
}

public class ContactSettingsDto
{
    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(11)]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست")]
    public string? Mobile { get; set; }

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(500)]
    public string? MapUrl { get; set; }

    [MaxLength(200)]
    public string? WorkHours { get; set; }
}

public class SocialSettingsDto
{
    [MaxLength(300)] public string? Instagram { get; set; }
    [MaxLength(300)] public string? Telegram { get; set; }
    [MaxLength(300)] public string? Facebook { get; set; }
    [MaxLength(300)] public string? Twitter { get; set; }
    [MaxLength(300)] public string? Youtube { get; set; }
    [MaxLength(300)] public string? Linkedin { get; set; }
    [MaxLength(300)] public string? Whatsapp { get; set; }
    [MaxLength(300)] public string? Aparat { get; set; }
}

public class ShippingSettingsDto
{
    [Range(0, 1_000_000_000)]
    public decimal FreeThreshold { get; set; } = 2_000_000m;

    [Range(0, 100_000_000)]
    public decimal NormalCost { get; set; } = 50_000m;

    [Range(0, 100_000_000)]
    public decimal ExpressCost { get; set; } = 120_000m;

    [Range(0, 100_000_000)]
    public decimal PickupCost { get; set; } = 0m;

    [MaxLength(50)]
    public string? NormalDays { get; set; }

    [MaxLength(500)]
    public string? ExpressCities { get; set; }
}

public class PaymentSettingsDto
{
    [MaxLength(100)]
    public string? ZarinpalMerchantId { get; set; }

    public bool ZarinpalSandbox { get; set; } = true;
    public bool CodEnabled { get; set; } = true;
    public bool OnlineEnabled { get; set; } = true;

    [Range(0, 100_000_000)]
    public decimal CodFee { get; set; }
}

public class SeoSettingsDto
{
    [MaxLength(70)] public string? DefaultTitle { get; set; }
    [MaxLength(160)] public string? DefaultDescription { get; set; }
    [MaxLength(300)] public string? DefaultKeywords { get; set; }
    [MaxLength(100)] public string? GoogleAnalytics { get; set; }
    [MaxLength(200)] public string? GoogleSearchConsole { get; set; }
    public bool RobotsEnabled { get; set; } = true;
}