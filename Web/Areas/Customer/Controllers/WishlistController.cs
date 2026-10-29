using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Application.Data;
using EShop.Domain.Entities;

namespace EShop.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class WishlistController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public WishlistController(IApplicationDbContext db, UserManager<AppUser> userManager)
    {
        _db = db; _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var items = await _db.WishlistItems
            .Where(w => w.UserId == user!.Id)
            .Include(w => w.Product).ThenInclude(p => p.Images)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int productId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { success = false, message = "لطفاً وارد شوید" });

        var existing = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

        if (existing != null)
        {
            _db.WishlistItems.Remove(existing);
            await _db.SaveChangesAsync();
            return Json(new { success = true, added = false, message = "از علاقه‌مندی حذف شد" });
        }

        _db.WishlistItems.Add(new WishlistItem
        {
            UserId = user.Id,
            ProductId = productId,
            AddedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Json(new { success = true, added = true, message = "به علاقه‌مندی اضافه شد" });
    }
}