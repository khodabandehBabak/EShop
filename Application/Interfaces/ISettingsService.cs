using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس تنظیمات سایت (Key-Value + Cache)
/// </summary>
public interface ISettingsService
{
    // Generic
    Task<string?> GetAsync(string key, string? defaultValue = null);
    Task<T> GetAsync<T>(string key, T defaultValue = default!);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0m);
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    Task SetAsync(string key, string? value, int? userId = null);

    // Typed
    Task<SiteSettingsVm> GetAllForEditAsync();
    Task SaveGeneralAsync(GeneralSettingsDto dto, int userId);
    Task SaveBrandingAsync(BrandingSettingsDto dto, int userId);
    Task SaveContactAsync(ContactSettingsDto dto, int userId);
    Task SaveSocialAsync(SocialSettingsDto dto, int userId);
    Task SaveShippingAsync(ShippingSettingsDto dto, int userId);
    Task SavePaymentAsync(PaymentSettingsDto dto, int userId);
    Task SaveSeoAsync(SeoSettingsDto dto, int userId);

    // Utility
    Task InvalidateCacheAsync();
    Task SeedDefaultsAsync();
}