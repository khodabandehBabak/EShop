using EShop.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace EShop.Application.Extensions;

public static class SettingsViewExtensions
{
    public static async Task<string?> GetSettingAsync(
        this ViewContext ctx, string key, string? def = null)
    {
        var svc = ctx.HttpContext.RequestServices.GetService<ISettingsService>();
        return svc == null ? def : await svc.GetAsync(key, def);
    }
}