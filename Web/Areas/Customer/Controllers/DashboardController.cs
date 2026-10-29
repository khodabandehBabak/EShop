using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Application.Data;
using EShop.Domain.Entities;
using EShop.Domain.Enums;

namespace EShop.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class DashboardController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public DashboardController(IApplicationDbContext db, UserManager<AppUser> userManager)
    {
        _db = db; _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var orders = _db.Orders.Where(o => o.CustomerId == user.Id);
        var userReviews = await _db.Reviews.Where(r => r.UserId == user.Id).ToListAsync();

        ViewBag.TotalOrders = await orders.CountAsync();
        ViewBag.PendingOrders = await orders.CountAsync(o =>
            o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);
        ViewBag.DeliveredOrders = await orders.CountAsync(o => o.Status == OrderStatus.Delivered);
        ViewBag.WishlistCount = await _db.WishlistItems.CountAsync(w => w.UserId == user.Id);
        ViewBag.TotalSpent = await orders
            .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Delivered)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        ViewBag.TotalReviews = userReviews.Count;
        ViewBag.ApprovedReviews = userReviews.Count(r => r.IsApproved);
        ViewBag.PendingReviews = userReviews.Count(r => !r.IsApproved &&
            string.IsNullOrEmpty(r.RejectionReason));

        ViewBag.RecentOrders = await orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5).ToListAsync();

        return View(user);
    }
}