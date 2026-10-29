using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class SettingsController : Controller
{
    private readonly ISettingsService _settings;
    private readonly UserManager<AppUser> _userManager;

    public SettingsController(ISettingsService settings, UserManager<AppUser> userManager)
    {
        _settings = settings; _userManager = userManager;
    }

    [HasPermission(PermissionCatalog.SettingsView)]
    public async Task<IActionResult> Index(string tab = "general")
    {
        ViewBag.ActiveTab = tab;
        var vm = await _settings.GetAllForEditAsync();
        return View(vm);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SettingsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGeneral(GeneralSettingsDto dto)
    {
        if (!ModelState.IsValid) return await ReturnWithErrorAsync("general", dto);
        await _settings.SaveGeneralAsync(dto, await CurrentUserIdAsync());
        TempData["Success"] = "تنظیمات عمومی ذخیره شد";
        return RedirectToAction(nameof(Index), new { tab = "general" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SettingsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBranding(BrandingSettingsDto dto)
    {
        try
        {
            await _settings.SaveBrandingAsync(dto, await CurrentUserIdAsync());
            TempData["Success"] = "لوگوها بروزرسانی شد";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "branding" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SettingsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveContact(ContactSettingsDto dto)
    {
        if (!ModelState.IsValid) return await ReturnWithErrorAsync("contact", dto);
        await _settings.SaveContactAsync(dto, await CurrentUserIdAsync());
        TempData["Success"] = "اطلاعات تماس ذخیره شد";
        return RedirectToAction(nameof(Index), new { tab = "contact" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SettingsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSocial(SocialSettingsDto dto)
    {
        if (!ModelState.IsValid) return await ReturnWithErrorAsync("social", dto);
        await _settings.SaveSocialAsync(dto, await CurrentUserIdAsync());
        TempData["Success"] = "شبکه‌های اجتماعی ذخیره شد";
        return RedirectToAction(nameof(Index), new { tab = "social" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SettingsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveShipping(ShippingSettingsDto dto)
    {
        if (!ModelState.IsValid) return await ReturnWithErrorAsync("shipping", dto);
        await _settings.SaveShippingAsync(dto, await CurrentUserIdAsync());
        TempData["Success"] = "تنظیمات حمل‌ونقل ذخیره شد";
        return RedirectToAction(nameof(Index), new { tab = "shipping" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SettingsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePayment(PaymentSettingsDto dto)
    {
        if (!ModelState.IsValid) return await ReturnWithErrorAsync("payment", dto);
        await _settings.SavePaymentAsync(dto, await CurrentUserIdAsync());
        TempData["Success"] = "تنظیمات پرداخت ذخیره شد";
        return RedirectToAction(nameof(Index), new { tab = "payment" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SettingsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSeo(SeoSettingsDto dto)
    {
        if (!ModelState.IsValid) return await ReturnWithErrorAsync("seo", dto);
        await _settings.SaveSeoAsync(dto, await CurrentUserIdAsync());
        TempData["Success"] = "تنظیمات سئو ذخیره شد";
        return RedirectToAction(nameof(Index), new { tab = "seo" });
    }

    private async Task<int> CurrentUserIdAsync()
        => int.Parse(_userManager.GetUserId(User)!);

    private async Task<IActionResult> ReturnWithErrorAsync<T>(string tab, T model)
    {
        var vm = await _settings.GetAllForEditAsync();
        switch (model)
        {
            case GeneralSettingsDto g: vm.General = g; break;
            case ContactSettingsDto c: vm.Contact = c; break;
            case SocialSettingsDto s: vm.Social = s; break;
            case ShippingSettingsDto sh: vm.Shipping = sh; break;
            case PaymentSettingsDto p: vm.Payment = p; break;
            case SeoSettingsDto se: vm.Seo = se; break;
        }
        ViewBag.ActiveTab = tab;
        return View("Index", vm);
    }
}