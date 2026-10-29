using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categories;
    public CategoriesController(ICategoryService categories) => _categories = categories;

    [HasPermission(PermissionCatalog.CategoriesView)]
    public async Task<IActionResult> Index(string? search, bool? isActive)
    {
        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        return View(await _categories.GetAllAsync(search, isActive));
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.CategoriesCreate)]
    public async Task<IActionResult> Create()
    {
        await FillParentsAsync();
        return View(new CategoryEditDto
        {
            IsActive = true,
            ShowInMenu = true,
            DisplayOrder = 100
        });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.CategoriesCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillParentsAsync(dto.ParentId);
            return View(dto);
        }
        try
        {
            await _categories.CreateAsync(dto);
            TempData["Success"] = "دسته‌بندی ایجاد شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillParentsAsync(dto.ParentId);
            return View(dto);
        }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.CategoriesEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _categories.GetForEditAsync(id);
        if (dto == null) return NotFound();
        await FillParentsAsync(dto.ParentId, id);
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.CategoriesEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillParentsAsync(dto.ParentId, dto.Id);
            return View(dto);
        }
        try
        {
            await _categories.UpdateAsync(dto);
            TempData["Success"] = "دسته‌بندی بروزرسانی شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillParentsAsync(dto.ParentId, dto.Id);
            return View(dto);
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.CategoriesDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _categories.DeleteAsync(id);
            return Json(new { success = ok, message = ok ? "حذف شد" : "یافت نشد" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.CategoriesEdit)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var val = await _categories.ToggleActiveAsync(id);
        return Json(new { success = true, isActive = val });
    }

    private async Task FillParentsAsync(int? selectedId = null, int? excludeId = null)
    {
        var tree = await _categories.GetTreeAsync();
        var items = new List<SelectListItem>
        {
            new() { Value = "", Text = "— بدون والد (سطح اصلی) —", Selected = selectedId == null }
        };
        Flatten(tree, items, 0, selectedId, excludeId);
        ViewBag.Parents = items;
    }

    private void Flatten(List<CategoryTreeItemDto> nodes, List<SelectListItem> items,
        int level, int? selectedId, int? excludeId)
    {
        foreach (var n in nodes)
        {
            if (excludeId.HasValue && n.Id == excludeId) continue;
            var prefix = level == 0 ? "" : new string('—', level) + " ";
            items.Add(new SelectListItem
            {
                Value = n.Id.ToString(),
                Text = prefix + n.Name,
                Selected = selectedId == n.Id
            });
            Flatten(n.Children, items, level + 1, selectedId, excludeId);
        }
    }
}