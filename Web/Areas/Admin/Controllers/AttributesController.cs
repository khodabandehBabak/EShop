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
public class AttributesController : Controller
{
    private readonly IProductAttributeService _service;
    public AttributesController(IProductAttributeService service) => _service = service;

    [HasPermission(PermissionCatalog.AttributesView)]
    public async Task<IActionResult> Index(string? search)
    {
        ViewBag.Search = search;
        return View(await _service.GetAllAsync(search));
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.AttributesCreate)]
    public IActionResult Create() =>
        View(new AttributeEditDto
        {
            IsFilterable = true,
            DisplayOrder = 100,
            Type = AttributeType.Select
        });

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttributeEditDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        try
        {
            var id = await _service.CreateAsync(dto);
            TempData["Success"] = "ویژگی ایجاد شد. اکنون مقادیر را تعریف کنید.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.AttributesEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _service.GetForEditAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AttributeEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            var c = await _service.GetForEditAsync(dto.Id);
            dto.Options = c?.Options ?? new();
            return View(dto);
        }
        try
        {
            await _service.UpdateAsync(dto);
            TempData["Success"] = "ویژگی بروزرسانی شد";
            return RedirectToAction(nameof(Edit), new { id = dto.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var c = await _service.GetForEditAsync(dto.Id);
            dto.Options = c?.Options ?? new();
            return View(dto);
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _service.DeleteAsync(id);
            return Json(new { success = ok, message = ok ? "حذف شد" : "یافت نشد" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesEdit)]
    public async Task<IActionResult> ToggleFilterable(int id)
    {
        var val = await _service.ToggleFilterableAsync(id);
        return Json(new { success = true, isFilterable = val });
    }

    // ============ Options ============

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOption(int attributeId, [FromForm] OptionCreateDto dto)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "اطلاعات نامعتبر" });
        try
        {
            var result = await _service.AddOptionAsync(attributeId, dto);
            return Json(new { success = true, option = result });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOption(int attributeId, [FromForm] OptionUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "اطلاعات نامعتبر" });
        try
        {
            var result = await _service.UpdateOptionAsync(attributeId, dto);
            return Json(new { success = true, option = result });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOption(int attributeId, int optionId)
    {
        try
        {
            var ok = await _service.DeleteOptionAsync(attributeId, optionId);
            return Json(new { success = ok, message = ok ? "حذف شد" : "یافت نشد" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.AttributesEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReorderOptions(int attributeId, [FromBody] List<int> orderedOptionIds)
    {
        if (orderedOptionIds == null || !orderedOptionIds.Any())
            return Json(new { success = false, message = "لیست خالی است" });
        try
        {
            await _service.ReorderOptionsAsync(attributeId, orderedOptionIds);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}