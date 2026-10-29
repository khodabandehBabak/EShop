using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Enums;
using EShop.Domain.Settings;

namespace EShop.Application.Services;

public class ShippingService : IShippingService
{
    private readonly ISettingsService _settings;

    public ShippingService(ISettingsService settings) => _settings = settings;

    public async Task<List<ShippingOptionDto>> GetOptionsAsync(decimal subTotal, string city)
    {
        var freeThreshold = await _settings.GetDecimalAsync(
            SettingsKeys.ShippingFreeThreshold, 2_000_000m);
        var normalCost = await _settings.GetDecimalAsync(
            SettingsKeys.ShippingNormalCost, 50_000m);
        var expressCost = await _settings.GetDecimalAsync(
            SettingsKeys.ShippingExpressCost, 120_000m);
        var pickupCost = await _settings.GetDecimalAsync(
            SettingsKeys.ShippingPickupCost, 0m);
        var normalDays = await _settings.GetAsync(
            SettingsKeys.ShippingNormalDays, "۳-۵ روز کاری");
        var expressCitiesRaw = await _settings.GetAsync(
            SettingsKeys.ShippingExpressCities, "تهران,کرج,اصفهان,مشهد,شیراز") ?? "";

        var isFreeNormal = subTotal >= freeThreshold;
        var expressCities = expressCitiesRaw.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var options = new List<ShippingOptionDto>
        {
            new()
            {
                Method = ShippingMethod.Normal,
                Title = "ارسال عادی (پست پیشتاز)",
                Description = isFreeNormal
                    ? "ارسال رایگان برای سفارش شما"
                    : normalDays ?? "",
                Cost = isFreeNormal ? 0m : normalCost,
                EstimatedDelivery = normalDays ?? ""
            },
            new()
            {
                Method = ShippingMethod.Express,
                Title = "ارسال سفارشی (پیک موتوری)",
                Description = "تحویل در همان روز یا روز کاری بعد",
                Cost = expressCost,
                EstimatedDelivery = "۲۴ ساعت"
            },
            new()
            {
                Method = ShippingMethod.Pickup,
                Title = "تحویل حضوری",
                Description = "از فروشگاه ستارخان - تهران",
                Cost = pickupCost,
                EstimatedDelivery = "بلافاصله پس از آماده‌سازی"
            }
        };

        // پیک موتوری فقط در شهرهای مجاز
        if (!string.IsNullOrWhiteSpace(city) &&
            !expressCities.Contains(city.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            options.RemoveAll(o => o.Method == ShippingMethod.Express);
        }

        return await Task.FromResult(options);
    }

    public async Task<decimal> GetCostAsync(ShippingMethod method, decimal subTotal)
    {
        return method switch
        {
            ShippingMethod.Normal => subTotal >= await _settings.GetDecimalAsync(
                SettingsKeys.ShippingFreeThreshold, 2_000_000m)
                ? 0m
                : await _settings.GetDecimalAsync(SettingsKeys.ShippingNormalCost, 50_000m),
            ShippingMethod.Express => await _settings.GetDecimalAsync(
                SettingsKeys.ShippingExpressCost, 120_000m),
            ShippingMethod.Pickup => await _settings.GetDecimalAsync(
                SettingsKeys.ShippingPickupCost, 0m),
            _ => await _settings.GetDecimalAsync(SettingsKeys.ShippingNormalCost, 50_000m)
        };
    }
}