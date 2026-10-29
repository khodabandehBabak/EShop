using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EShop.Application.Data;
using EShop.Domain.Entities;

namespace EShop.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class AddressController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public AddressController(IApplicationDbContext db, UserManager<AppUser> userManager)
    {
        _db = db; _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var addresses = await _db.Addresses
            .Where(a => a.UserId == user!.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return View(addresses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Address input)
    {
        var user = await _userManager.GetUserAsync(User);
        input.UserId = user!.Id;
        _db.Addresses.Add(input);
        await _db.SaveChangesAsync();
        TempData["Success"] = "آدرس جدید ذخیره شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var address = await _db.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user!.Id);
        if (address == null) return NotFound();

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();
        TempData["Success"] = "آدرس حذف شد";
        return RedirectToAction(nameof(Index));
    }
}