using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Settings;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EShop.Application.Services;

public class SettingsService : ISettingsService
{
    private const string CacheKey = "EShop:Settings";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IFileService _files;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(
        IApplicationDbContext db, IMemoryCache cache,
        IFileService files, ILogger<SettingsService> logger)
    {
        _db = db; _cache = cache; _files = files; _logger = logger;
    }

    private async Task<Dictionary<string, string?>> GetAllCachedAsync()
    {
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, string?>? cached) && cached != null)
            return cached;

        var dict = await _db.SiteSettings
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        _cache.Set(CacheKey, dict, CacheDuration);
        return dict;
    }

    public async Task<string?> GetAsync(string key, string? defaultValue = null)
    {
        var dict = await GetAllCachedAsync();
        return dict.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val)
            ? val : defaultValue;
    }

    public async Task<T> GetAsync<T>(string key, T defaultValue = default!)
    {
        var raw = await GetAsync(key);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        try { return (T)Convert.ChangeType(raw, typeof(T)); }
        catch { return defaultValue; }
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var raw = await GetAsync(key);
        return bool.TryParse(raw, out var v) ? v : defaultValue;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0m)
    {
        var raw = await GetAsync(key);
        return decimal.TryParse(raw, out var v) ? v : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var raw = await GetAsync(key);
        return int.TryParse(raw, out var v) ? v : defaultValue;
    }

    public async Task SetAsync(string key, string? value, int? userId = null)
    {
        var setting = await _db.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _db.SiteSettings.Add(new SiteSetting
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow,
                UpdatedByUserId = userId
            });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedByUserId = userId;
        }
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }

    public Task InvalidateCacheAsync()
    {
        _cache.Remove(CacheKey);
        return Task.CompletedTask;
    }

    public async Task<SiteSettingsVm> GetAllForEditAsync()
    {
        var dict = await GetAllCachedAsync();
        string? G(string k) => dict.TryGetValue(k, out var v) ? v : null;
        bool B(string k, bool d = false) => bool.TryParse(G(k), out var b) ? b : d;
        decimal D(string k, decimal d = 0) => decimal.TryParse(G(k), out var x) ? x : d;
        int I(string k, int d = 0) => int.TryParse(G(k), out var x) ? x : d;

        return new SiteSettingsVm
        {
            General = new GeneralSettingsDto
            {
                SiteName = G(SettingsKeys.SiteName) ?? "فروشگاه ستارخان",
                SiteTagline = G(SettingsKeys.SiteTagline),
                SiteDescription = G(SettingsKeys.SiteDescription),
                SiteKeywords = G(SettingsKeys.SiteKeywords),
                Currency = G(SettingsKeys.DefaultCurrency) ?? "تومان",
                TimeZone = G(SettingsKeys.TimeZone) ?? "Asia/Tehran"
            },
            Branding = new BrandingSettingsDto
            {
                ExistingLogoUrl = G(SettingsKeys.LogoUrl),
                ExistingFooterLogoUrl = G(SettingsKeys.FooterLogoUrl),
                ExistingFaviconUrl = G(SettingsKeys.FaviconUrl),
                LogoWidth = I(SettingsKeys.LogoWidth, 144),
                LogoHeight = I(SettingsKeys.LogoHeight, 45)
            },
            Contact = new ContactSettingsDto
            {
                Phone = G(SettingsKeys.ContactPhone),
                Mobile = G(SettingsKeys.ContactMobile),
                Email = G(SettingsKeys.ContactEmail),
                Address = G(SettingsKeys.ContactAddress),
                PostalCode = G(SettingsKeys.ContactPostalCode),
                MapUrl = G(SettingsKeys.ContactMapUrl),
                WorkHours = G(SettingsKeys.ContactWorkHours)
            },
            Social = new SocialSettingsDto
            {
                Instagram = G(SettingsKeys.SocialInstagram),
                Telegram = G(SettingsKeys.SocialTelegram),
                Facebook = G(SettingsKeys.SocialFacebook),
                Twitter = G(SettingsKeys.SocialTwitter),
                Youtube = G(SettingsKeys.SocialYoutube),
                Linkedin = G(SettingsKeys.SocialLinkedin),
                Whatsapp = G(SettingsKeys.SocialWhatsapp),
                Aparat = G(SettingsKeys.SocialAparat)
            },
            Shipping = new ShippingSettingsDto
            {
                FreeThreshold = D(SettingsKeys.ShippingFreeThreshold, 2_000_000m),
                NormalCost = D(SettingsKeys.ShippingNormalCost, 50_000m),
                ExpressCost = D(SettingsKeys.ShippingExpressCost, 120_000m),
                PickupCost = D(SettingsKeys.ShippingPickupCost, 0m),
                NormalDays = G(SettingsKeys.ShippingNormalDays),
                ExpressCities = G(SettingsKeys.ShippingExpressCities)
            },
            Payment = new PaymentSettingsDto
            {
                ZarinpalMerchantId = G(SettingsKeys.PaymentZarinpalMerchantId),
                ZarinpalSandbox = B(SettingsKeys.PaymentZarinpalSandbox, true),
                CodEnabled = B(SettingsKeys.PaymentCodEnabled, true),
                OnlineEnabled = B(SettingsKeys.PaymentOnlineEnabled, true),
                CodFee = D(SettingsKeys.PaymentCodFee, 0m)
            },
            Seo = new SeoSettingsDto
            {
                DefaultTitle = G(SettingsKeys.SeoDefaultTitle),
                DefaultDescription = G(SettingsKeys.SeoDefaultDescription),
                DefaultKeywords = G(SettingsKeys.SeoDefaultKeywords),
                GoogleAnalytics = G(SettingsKeys.SeoGoogleAnalytics),
                GoogleSearchConsole = G(SettingsKeys.SeoGoogleSearchConsole),
                RobotsEnabled = B(SettingsKeys.SeoRobotsEnabled, true)
            }
        };
    }

    public async Task SaveGeneralAsync(GeneralSettingsDto dto, int userId)
        => await UpsertManyAsync(new Dictionary<string, string?>
        {
            [SettingsKeys.SiteName] = dto.SiteName?.Trim(),
            [SettingsKeys.SiteTagline] = dto.SiteTagline?.Trim(),
            [SettingsKeys.SiteDescription] = dto.SiteDescription?.Trim(),
            [SettingsKeys.SiteKeywords] = dto.SiteKeywords?.Trim(),
            [SettingsKeys.DefaultCurrency] = dto.Currency?.Trim(),
            [SettingsKeys.TimeZone] = dto.TimeZone?.Trim()
        }, userId);

    public async Task SaveBrandingAsync(BrandingSettingsDto dto, int userId)
    {
        var updates = new Dictionary<string, string?>();

        if (dto.LogoFile != null)
        {
            await _files.DeleteAsync(await GetAsync(SettingsKeys.LogoUrl));
            updates[SettingsKeys.LogoUrl] = await _files.SaveImageAsync(dto.LogoFile, "site", 500);
        }
        if (dto.FooterLogoFile != null)
        {
            await _files.DeleteAsync(await GetAsync(SettingsKeys.FooterLogoUrl));
            updates[SettingsKeys.FooterLogoUrl] = await _files.SaveImageAsync(dto.FooterLogoFile, "site", 500);
        }
        if (dto.FaviconFile != null)
        {
            await _files.DeleteAsync(await GetAsync(SettingsKeys.FaviconUrl));
            updates[SettingsKeys.FaviconUrl] = await _files.SaveImageAsync(dto.FaviconFile, "site", 128);
        }

        updates[SettingsKeys.LogoWidth] = dto.LogoWidth?.ToString();
        updates[SettingsKeys.LogoHeight] = dto.LogoHeight?.ToString();

        await UpsertManyAsync(updates, userId);
    }

    public async Task SaveContactAsync(ContactSettingsDto dto, int userId)
        => await UpsertManyAsync(new Dictionary<string, string?>
        {
            [SettingsKeys.ContactPhone] = dto.Phone?.Trim(),
            [SettingsKeys.ContactMobile] = dto.Mobile?.Trim(),
            [SettingsKeys.ContactEmail] = dto.Email?.Trim(),
            [SettingsKeys.ContactAddress] = dto.Address?.Trim(),
            [SettingsKeys.ContactPostalCode] = dto.PostalCode?.Trim(),
            [SettingsKeys.ContactMapUrl] = dto.MapUrl?.Trim(),
            [SettingsKeys.ContactWorkHours] = dto.WorkHours?.Trim()
        }, userId);

    public async Task SaveSocialAsync(SocialSettingsDto dto, int userId)
        => await UpsertManyAsync(new Dictionary<string, string?>
        {
            [SettingsKeys.SocialInstagram] = dto.Instagram?.Trim(),
            [SettingsKeys.SocialTelegram] = dto.Telegram?.Trim(),
            [SettingsKeys.SocialFacebook] = dto.Facebook?.Trim(),
            [SettingsKeys.SocialTwitter] = dto.Twitter?.Trim(),
            [SettingsKeys.SocialYoutube] = dto.Youtube?.Trim(),
            [SettingsKeys.SocialLinkedin] = dto.Linkedin?.Trim(),
            [SettingsKeys.SocialWhatsapp] = dto.Whatsapp?.Trim(),
            [SettingsKeys.SocialAparat] = dto.Aparat?.Trim()
        }, userId);

    public async Task SaveShippingAsync(ShippingSettingsDto dto, int userId)
        => await UpsertManyAsync(new Dictionary<string, string?>
        {
            [SettingsKeys.ShippingFreeThreshold] = dto.FreeThreshold.ToString(),
            [SettingsKeys.ShippingNormalCost] = dto.NormalCost.ToString(),
            [SettingsKeys.ShippingExpressCost] = dto.ExpressCost.ToString(),
            [SettingsKeys.ShippingPickupCost] = dto.PickupCost.ToString(),
            [SettingsKeys.ShippingNormalDays] = dto.NormalDays?.Trim(),
            [SettingsKeys.ShippingExpressCities] = dto.ExpressCities?.Trim()
        }, userId);

    public async Task SavePaymentAsync(PaymentSettingsDto dto, int userId)
        => await UpsertManyAsync(new Dictionary<string, string?>
        {
            [SettingsKeys.PaymentZarinpalMerchantId] = dto.ZarinpalMerchantId?.Trim(),
            [SettingsKeys.PaymentZarinpalSandbox] = dto.ZarinpalSandbox.ToString().ToLowerInvariant(),
            [SettingsKeys.PaymentCodEnabled] = dto.CodEnabled.ToString().ToLowerInvariant(),
            [SettingsKeys.PaymentOnlineEnabled] = dto.OnlineEnabled.ToString().ToLowerInvariant(),
            [SettingsKeys.PaymentCodFee] = dto.CodFee.ToString()
        }, userId);

    public async Task SaveSeoAsync(SeoSettingsDto dto, int userId)
        => await UpsertManyAsync(new Dictionary<string, string?>
        {
            [SettingsKeys.SeoDefaultTitle] = dto.DefaultTitle?.Trim(),
            [SettingsKeys.SeoDefaultDescription] = dto.DefaultDescription?.Trim(),
            [SettingsKeys.SeoDefaultKeywords] = dto.DefaultKeywords?.Trim(),
            [SettingsKeys.SeoGoogleAnalytics] = dto.GoogleAnalytics?.Trim(),
            [SettingsKeys.SeoGoogleSearchConsole] = dto.GoogleSearchConsole?.Trim(),
            [SettingsKeys.SeoRobotsEnabled] = dto.RobotsEnabled.ToString().ToLowerInvariant()
        }, userId);

    private async Task UpsertManyAsync(Dictionary<string, string?> values, int userId)
    {
        var existing = await _db.SiteSettings
            .Where(s => values.Keys.Contains(s.Key))
            .ToListAsync();

        foreach (var (key, val) in values)
        {
            var setting = existing.FirstOrDefault(s => s.Key == key);
            if (setting == null)
            {
                _db.SiteSettings.Add(new SiteSetting
                {
                    Key = key,
                    Value = val,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = userId
                });
            }
            else
            {
                setting.Value = val;
                setting.UpdatedAt = DateTime.UtcNow;
                setting.UpdatedByUserId = userId;
            }
        }

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }

    public async Task SeedDefaultsAsync()
    {
        var existing = await _db.SiteSettings.Select(s => s.Key).ToListAsync();
        var set = existing.ToHashSet();

        var defaults = new Dictionary<string, string?>
        {
            [SettingsKeys.SiteName] = "فروشگاه ستارخان",
            [SettingsKeys.SiteTagline] = "خرید آنلاین با بهترین قیمت",
            [SettingsKeys.DefaultCurrency] = "تومان",
            [SettingsKeys.TimeZone] = "Asia/Tehran",
            [SettingsKeys.LogoWidth] = "144",
            [SettingsKeys.LogoHeight] = "45",
            [SettingsKeys.ContactWorkHours] = "شنبه تا پنجشنبه ۹ تا ۱۸",
            [SettingsKeys.ShippingFreeThreshold] = "2000000",
            [SettingsKeys.ShippingNormalCost] = "50000",
            [SettingsKeys.ShippingExpressCost] = "120000",
            [SettingsKeys.ShippingPickupCost] = "0",
            [SettingsKeys.ShippingNormalDays] = "۳-۵ روز کاری",
            [SettingsKeys.ShippingExpressCities] = "تهران,کرج,اصفهان,مشهد,شیراز",
            [SettingsKeys.PaymentZarinpalSandbox] = "true",
            [SettingsKeys.PaymentCodEnabled] = "true",
            [SettingsKeys.PaymentOnlineEnabled] = "true",
            [SettingsKeys.PaymentCodFee] = "0",
            [SettingsKeys.SeoDefaultTitle] = "فروشگاه ستارخان | خرید آنلاین",
            [SettingsKeys.SeoDefaultDescription] = "خرید آنلاین با بهترین قیمت و ارسال سریع",
            [SettingsKeys.SeoRobotsEnabled] = "true",
            [SettingsKeys.MaintenanceMode] = "false"
        };

        foreach (var (key, val) in defaults)
        {
            if (set.Contains(key)) continue;
            _db.SiteSettings.Add(new SiteSetting { Key = key, Value = val });
        }

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }
}