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
public class ReviewsController : Controller
{
    private readonly IReviewService _reviews;
    private readonly UserManager<AppUser> _userManager;

    public ReviewsController(IReviewService reviews, UserManager<AppUser> userManager)
    {
        _reviews = reviews; _userManager = userManager;
    }

    [HasPermission(PermissionCatalog.ReviewsView)]
    public async Task<IActionResult> Index(int? productId, bool? isApproved,
        int? minRating, string? search, int page = 1)
    {
        var filter = new ReviewFilterDto
        {
            ProductId = productId,
            IsApproved = isApproved,
            MinRating = minRating,
            Search = search,
            Page = Math.Max(1, page),
            PageSize = 20
        };

        var (items, total) = await _reviews.GetForAdminAsync(filter);
        ViewBag.Filter = filter;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize);
        return View(items);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ReviewsModerate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        await _reviews.ApproveAsync(id, user!.Id);
        return Json(new { success = true, message = "نظر تأیید شد" });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ReviewsModerate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var user = await _userManager.GetUserAsync(User);
        var (ok, msg) = await _reviews.RejectAsync(id, reason, user!.Id);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.ReviewsDelete)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviews.DeleteAsync(id);
        return Json(new { success = true, message = "نظر حذف شد" });
    }
}