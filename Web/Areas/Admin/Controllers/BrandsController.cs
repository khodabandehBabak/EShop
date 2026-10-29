using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class BrandsController : Controller
{
    private readonly IBrandService _brands;
    public BrandsController(IBrandService brands) => _brands = brands;

    [HasPermission(PermissionCatalog.BrandsView)]
    public async Task<IActionResult> Index(string? search, bool? isActive)
    {
        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        return View(await _brands.GetAllAsync(search, isActive));
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.BrandsCreate)]
    public IActionResult Create() =>
        View(new BrandEditDto { IsActive = true, DisplayOrder = 100 });

    [HttpPost]
    [HasPermission(PermissionCatalog.BrandsCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BrandEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _brands.CreateAsync(dto);
            TempData["Success"] = "برند ایجاد شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.BrandsEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _brands.GetForEditAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.BrandsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BrandEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            await _brands.UpdateAsync(dto);
            TempData["Success"] = "برند بروزرسانی شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.BrandsDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _brands.DeleteAsync(id);
            return Json(new { success = ok, message = ok ? "حذف شد" : "یافت نشد" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.BrandsEdit)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var val = await _brands.ToggleActiveAsync(id);
        return Json(new { success = true, isActive = val });
    }
}