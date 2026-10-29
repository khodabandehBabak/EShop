using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class RolesController : Controller
{
    private readonly IRoleManagementService _roles;
    private readonly IPermissionService _perms;

    public RolesController(IRoleManagementService roles, IPermissionService perms)
    {
        _roles = roles; _perms = perms;
    }

    [HasPermission(PermissionCatalog.RolesView)]
    public async Task<IActionResult> Index() => View(await _roles.GetAllAsync());

    [HttpGet]
    [HasPermission(PermissionCatalog.RolesCreate)]
    public async Task<IActionResult> Create()
    {
        await FillLookupsAsync();
        return View(new RoleCreateDto());
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.RolesCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(dto.PermissionIds);
            return View(dto);
        }
        try
        {
            await _roles.CreateAsync(dto);
            TempData["Success"] = "نقش ایجاد شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillLookupsAsync(dto.PermissionIds);
            return View(dto);
        }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.RolesEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _roles.GetForEditAsync(id);
        if (dto == null) return NotFound();
        await FillLookupsAsync(dto.PermissionIds);
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.RolesEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(dto.PermissionIds);
            return View(dto);
        }
        try
        {
            await _roles.UpdateAsync(dto);
            TempData["Success"] = "نقش بروزرسانی شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillLookupsAsync(dto.PermissionIds);
            return View(dto);
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.RolesDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _roles.DeleteAsync(id);
            return Json(new { success = ok, message = ok ? "حذف شد" : "یافت نشد" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task FillLookupsAsync(List<int>? selectedIds = null)
    {
        ViewBag.PermissionGroups = await _perms.GetAllGroupedAsync();
        ViewBag.SelectedPermissionIds = (selectedIds ?? new()).ToHashSet();
    }
}