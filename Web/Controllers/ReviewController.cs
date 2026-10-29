using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;

namespace EShop.Web.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewService _reviews;
    private readonly UserManager<AppUser> _userManager;

    public ReviewController(IReviewService reviews, UserManager<AppUser> userManager)
    {
        _reviews = reviews;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(int productId, int page = 1, int pageSize = 10)
    {
        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            userId = user?.Id;
        }

        var result = await _reviews.GetForProductAsync(productId, page, pageSize, userId);
        return Json(new
        {
            success = true,
            page = result.Page,
            totalPages = result.TotalPages,
            total = result.TotalCount,
            avg = result.AvgRating,
            items = result.Items,
            hasMore = result.Page < result.TotalPages
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("reviews")]
    public async Task<IActionResult> Submit(SubmitReviewDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join("، ", errors) });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "لطفاً وارد شوید" });

        var (ok, msg) = await _reviews.SubmitAsync(dto, user.Id);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("reviews")]
    public async Task<IActionResult> Vote(int reviewId, bool isHelpful)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "لطفاً وارد شوید" });

        var (ok, msg) = await _reviews.VoteAsync(reviewId, user.Id, isHelpful);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOwn(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "لطفاً وارد شوید" });

        var (ok, msg) = await _reviews.DeleteOwnAsync(id, user.Id);
        return Json(new { success = ok, message = msg });
    }

    [HttpGet("/review/edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var dto = await _reviews.GetForEditAsync(id, user.Id);
        if (dto == null) return NotFound();

        if (!dto.CanStillEdit)
            return RedirectToAction("Index", "Reviews", new { area = "Customer" });

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MyReviewEditDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join("، ", errors) });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "لطفاً وارد شوید" });

        var (ok, msg) = await _reviews.UpdateAsync(dto, user.Id);
        return Json(new { success = ok, message = msg });
    }
}