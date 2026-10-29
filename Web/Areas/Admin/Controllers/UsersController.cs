using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class UsersController : Controller
{
    private readonly IUserManagementService _users;
    private readonly IRoleManagementService _roles;
    private readonly IPermissionService _perms;
    private readonly UserManager<AppUser> _userManager;

    public UsersController(
        IUserManagementService users,
        IRoleManagementService roles,
        IPermissionService perms,
        UserManager<AppUser> userManager)
    {
        _users = users; _roles = roles; _perms = perms; _userManager = userManager;
    }

    [HasPermission(PermissionCatalog.UsersView)]
    public async Task<IActionResult> Index(string? search, string? role, bool? isActive, int page = 1)
    {
        const int pageSize = 20;
        var (items, total) = await _users.GetAllAsync(search, role, isActive, page, pageSize);

        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.IsActive = isActive;
        ViewBag.Page = page;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        var roles = await _roles.GetAllAsync();
        ViewBag.Roles = roles.Select(r => new SelectListItem
        {
            Value = r.Name,
            Text = r.Name,
            Selected = r.Name == role
        }).ToList();

        return View(items);
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.UsersCreate)]
    public async Task<IActionResult> Create()
    {
        await FillLookupsAsync();
        return View(new UserCreateDto { IsActive = true });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.UsersCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(dto.RoleIds, dto.DirectPermissionIds);
            return View(dto);
        }
        try
        {
            await _users.CreateAsync(dto);
            TempData["Success"] = "کاربر ایجاد شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillLookupsAsync(dto.RoleIds, dto.DirectPermissionIds);
            return View(dto);
        }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.UsersEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _users.GetForEditAsync(id);
        if (dto == null) return NotFound();

        var currentUserId = int.Parse(_userManager.GetUserId(User)!);
        dto.IsSelf = dto.Id == currentUserId;

        await FillLookupsAsync(dto.RoleIds, dto.DirectPermissionIds);
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.UsersEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(dto.RoleIds, dto.DirectPermissionIds);
            return View(dto);
        }
        try
        {
            var currentUserId = int.Parse(_userManager.GetUserId(User)!);
            await _users.UpdateAsync(dto, currentUserId);
            TempData["Success"] = "کاربر بروزرسانی شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillLookupsAsync(dto.RoleIds, dto.DirectPermissionIds);
            return View(dto);
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.UsersDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var currentUserId = int.Parse(_userManager.GetUserId(User)!);
            var ok = await _users.DeleteAsync(id, currentUserId);
            return Json(new { success = ok, message = ok ? "کاربر حذف/غیرفعال شد" : "یافت نشد" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.UsersEdit)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        try
        {
            var currentUserId = int.Parse(_userManager.GetUserId(User)!);
            var val = await _users.ToggleActiveAsync(id, currentUserId);
            return Json(new { success = true, isActive = val });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task FillLookupsAsync(List<int>? roleIds = null, List<int>? permIds = null)
    {
        roleIds ??= new();
        permIds ??= new();

        var roles = await _roles.GetAllAsync();
        ViewBag.Roles = roles.Select(r => new SelectListItem
        {
            Value = r.Id.ToString(),
            Text = r.Name,
            Selected = roleIds.Contains(r.Id)
        }).ToList();

        ViewBag.PermissionGroups = await _perms.GetAllGroupedAsync();
        ViewBag.SelectedPermissionIds = permIds.ToHashSet();
    }
}