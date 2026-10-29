using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EShop.Domain.Entities;

namespace EShop.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerOnly")]
public class ProfileController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public ProfileController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        _userManager = userManager; _signInManager = signInManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        string firstName, string lastName, string? nationalCode, string email)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        user.FirstName = firstName?.Trim() ?? "";
        user.LastName = lastName?.Trim() ?? "";
        user.NationalCode = nationalCode?.Trim();
        user.Email = email?.Trim();

        await _userManager.UpdateAsync(user);
        TempData["Success"] = "اطلاعات بروزرسانی شد";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "رمز جدید با تکرار آن همخوانی ندارد";
            return RedirectToAction(nameof(Index));
        }
        if (newPassword.Length < 6)
        {
            TempData["Error"] = "رمز باید حداقل ۶ کاراکتر باشد";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        var result = await _userManager.ChangePasswordAsync(user!, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("، ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _signInManager.RefreshSignInAsync(user!);
        TempData["Success"] = "رمز عبور تغییر کرد";
        return RedirectToAction(nameof(Index));
    }
}