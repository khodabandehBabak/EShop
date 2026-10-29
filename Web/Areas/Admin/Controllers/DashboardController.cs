using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Application.Data;
using EShop.Domain.Enums;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class DashboardController : Controller
{
    private readonly IApplicationDbContext _db;
    public DashboardController(IApplicationDbContext db) => _db = db;

    [HasPermission(PermissionCatalog.DashboardView)]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddDays(-30);

        var successStatuses = new[]
        {
            OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Packed,
            OrderStatus.Shipped, OrderStatus.Delivered
        };

        ViewBag.TodayOrders = await _db.Orders.CountAsync(o => o.CreatedAt >= today);
        ViewBag.TodayRevenue = await _db.Orders
            .Where(o => o.CreatedAt >= today && successStatuses.Contains(o.Status))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.WeekRevenue = await _db.Orders
            .Where(o => o.CreatedAt >= weekAgo && successStatuses.Contains(o.Status))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.MonthRevenue = await _db.Orders
            .Where(o => o.CreatedAt >= monthAgo && successStatuses.Contains(o.Status))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        ViewBag.TotalProducts = await _db.Products.CountAsync(p => p.IsActive);
        ViewBag.TotalCustomers = await _db.Users.CountAsync();
        ViewBag.PendingOrders = await _db.Orders
            .CountAsync(o => o.Status == OrderStatus.Pending
                          || o.Status == OrderStatus.Processing);
        ViewBag.TodayViews = await _db.Products
            .Where(p => p.LastViewAt >= today)
            .SumAsync(p => (int?)p.ViewCount) ?? 0;

        // نمودار ۷ روز
        var weekData = new List<object>();
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var amount = await _db.Orders
                .Where(o => o.CreatedAt.Date == day && successStatuses.Contains(o.Status))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            weekData.Add(new { day = day.ToString("MM/dd"), amount });
        }
        ViewBag.WeekChart = weekData;

        // Top viewed
        ViewBag.TopViewedProducts = await _db.Products
            .Where(p => p.IsActive && p.ViewCount > 0)
            .OrderByDescending(p => p.ViewCount)
            .Take(5)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Slug,
                p.ViewCount,
                ImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
            })
            .ToListAsync();

        var recentOrders = await _db.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10).ToListAsync();

        return View(recentOrders);
    }
}