using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;

namespace EShop.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class ReviewsController : Controller
{
    private readonly IReviewService _reviews;
    private readonly UserManager<AppUser> _userManager;

    public ReviewsController(IReviewService reviews, UserManager<AppUser> userManager)
    {
        _reviews = reviews; _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? status, string? search, int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _reviews.GetMyReviewsAsync(
            user.Id,
            statusFilter: status,
            search: search,
            page: Math.Max(1, page),
            pageSize: 10);

        return View(result);
    }
}