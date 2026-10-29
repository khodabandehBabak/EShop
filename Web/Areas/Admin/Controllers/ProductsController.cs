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
public class ProductsController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IBrandService _brands;

    public ProductsController(IProductService products,
        ICategoryService categories, IBrandService brands)
    {
        _products = products; _categories = categories; _brands = brands;
    }

    [HasPermission(PermissionCatalog.ProductsView)]
    public async Task<IActionResult> Index(string? search, int? categoryId,
        int? brandId, bool? isActive, int page = 1)
    {
        const int pageSize = 20;
        var (items, total) = await _products.GetAllAsync(
            search, categoryId, brandId, isActive, page, pageSize);

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.BrandId = brandId;
        ViewBag.IsActive = isActive;
        ViewBag.Page = page;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        await FillDropdownsAsync(categoryId, brandId);
        return View(items);
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.ProductsCreate)]
    public async Task<IActionResult> Create()
    {
        await FillFormDropdownsAsync();
        return View(new ProductEditDto { IsActive = true, LowStockThreshold = 5 });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ProductsCreate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillFormDropdownsAsync(dto.CategoryId, dto.BrandId);
            return View(dto);
        }
        try
        {
            await _products.CreateAsync(dto);
            TempData["Success"] = "محصول با موفقیت ایجاد شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillFormDropdownsAsync(dto.CategoryId, dto.BrandId);
            return View(dto);
        }
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.ProductsEdit)]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _products.GetForEditAsync(id);
        if (dto == null) return NotFound();
        await FillFormDropdownsAsync(dto.CategoryId, dto.BrandId);
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ProductsEdit)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            await FillFormDropdownsAsync(dto.CategoryId, dto.BrandId);
            return View(dto);
        }
        try
        {
            await _products.UpdateAsync(dto);
            TempData["Success"] = "محصول بروزرسانی شد";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await FillFormDropdownsAsync(dto.CategoryId, dto.BrandId);
            return View(dto);
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ProductsDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var ok = await _products.DeleteAsync(id);
            return Json(new { success = ok, message = ok ? "حذف شد" : "یافت نشد" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ProductsEdit)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var val = await _products.ToggleActiveAsync(id);
        return Json(new { success = true, isActive = val });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ProductsEdit)]
    public async Task<IActionResult> ToggleFeatured(int id)
    {
        var val = await _products.ToggleFeaturedAsync(id);
        return Json(new { success = true, isFeatured = val });
    }

    private async Task FillDropdownsAsync(int? categoryId = null, int? brandId = null)
    {
        var cats = await _categories.GetAllAsync();
        ViewBag.Categories = cats.Select(c => new SelectListItem
        {
            Value = c.Id.ToString(),
            Text = c.Name,
            Selected = c.Id == categoryId
        }).ToList();

        var brands = await _brands.GetAllAsync();
        ViewBag.Brands = brands.Select(b => new SelectListItem
        {
            Value = b.Id.ToString(),
            Text = b.Name,
            Selected = b.Id == brandId
        }).ToList();
    }

    private async Task FillFormDropdownsAsync(int? categoryId = null, int? brandId = null)
    {
        await FillDropdownsAsync(categoryId, brandId);
        ViewBag.Attributes = await _products.GetAttributesAsync();
    }
}