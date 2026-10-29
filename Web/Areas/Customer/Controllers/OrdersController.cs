using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Application.Data;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;

namespace EShop.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class OrdersController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly IOrderWorkflowService _workflow;
    private readonly UserManager<AppUser> _userManager;

    public OrdersController(
        IApplicationDbContext db,
        IOrderWorkflowService workflow,
        UserManager<AppUser> userManager)
    {
        _db = db; _workflow = workflow; _userManager = userManager;
    }

    public async Task<IActionResult> Index(OrderStatus? status, int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        const int pageSize = 10;

        var query = _db.Orders.Where(o => o.CustomerId == user!.Id);
        if (status.HasValue) query = query.Where(o => o.Status == status);

        var total = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.Items)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total = total;

        return View(orders);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory.OrderBy(h => h.ChangedAt))
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == user!.Id);

        if (order == null) return NotFound();
        return View(order);
    }

    public async Task<IActionResult> Track(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var tracking = await _workflow.GetTrackingAsync(id, user!.Id);
        if (tracking == null) return NotFound();
        return View(tracking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == user!.Id);

        if (order == null) return NotFound();

        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "این سفارش قابل لغو نیست. تنها سفارش‌های در انتظار پرداخت قابل لغو هستند.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Cancelled,
            Note = "لغو توسط مشتری",
            ChangedAt = DateTime.UtcNow
        });

        // بازگرداندن موجودی
        var items = await _db.OrderItems.Where(i => i.OrderId == id).ToListAsync();
        foreach (var item in items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null) product.Stock += item.Quantity;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "سفارش شما لغو شد";
        return RedirectToAction(nameof(Index));
    }
}