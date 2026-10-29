using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Enums;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class SlidersController : Controller
{
    private readonly ISliderService _sliders;
    private readonly IHomeService _home;

    public SlidersController(ISliderService sliders, IHomeService home)
    {
        _sliders = sliders; _home = home;
    }

    [HasPermission(PermissionCatalog.SlidersView)]
    public async Task<IActionResult> Index(SliderPosition? position)
    {
        ViewBag.Position = position;
        return View(await _sliders.GetAllAsync(position));
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.SlidersCreate)]
    public IActionResult Create() => View(new SliderEditDto
    {
        IsActive = true,
        DisplayOrder = 100,
        BgColor = "#f1f0f0",
        TextColor = "#333"
    });

    [HttpPost]
    [HasPermission(PermissionCatalog.SlidersCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SliderEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _sliders.CreateAsync(dto);
            await _home.InvalidateCacheAsync();
            TempData["Success"] = "اسلایدر ایجاد شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.SlidersEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _sliders.GetForEditAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SlidersEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SliderEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _sliders.UpdateAsync(dto);
            await _home.InvalidateCacheAsync();
            TempData["Success"] = "اسلایدر بروزرسانی شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SlidersDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _sliders.DeleteAsync(id);
        if (ok) await _home.InvalidateCacheAsync();
        return Json(new { success = ok, message = ok ? "حذف شد" : "یافت نشد" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.SlidersEdit)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var val = await _sliders.ToggleActiveAsync(id);
        await _home.InvalidateCacheAsync();
        return Json(new { success = true, isActive = val });
    }
}