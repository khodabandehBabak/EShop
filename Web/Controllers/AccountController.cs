using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;

namespace EShop.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IOtpService _otp;

    public AccountController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        IOtpService otp)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _otp = otp;
    }

    // ============================================================
    // CUSTOMER: OTP Login
    // ============================================================

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home", new { area = "" });

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> RequestOtp(string phoneNumber, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) ||
            !System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^09\d{9}$"))
        {
            TempData["Error"] = "شماره موبایل معتبر نیست";
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        var code = await _otp.GenerateAndSendAsync(phoneNumber);

        // در حالت توسعه، کد را در TempData نمایش می‌دهیم
        if (HttpContext.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            TempData["DevOtp"] = code;

        return RedirectToAction(nameof(VerifyOtp),
            new { phone = phoneNumber, returnUrl });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string phone, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return RedirectToAction(nameof(Login));

        ViewBag.Phone = phone;
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> VerifyOtp(
        string phone, string code, string? returnUrl = null)
    {
        var isValid = await _otp.VerifyAsync(phone, code);
        if (!isValid)
        {
            TempData["Error"] = "کد وارد شده نامعتبر یا منقضی شده است";
            ViewBag.Phone = phone;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // پیدا کردن یا ایجاد کاربر
        var user = await _userManager.FindByNameAsync(phone);
        if (user == null)
        {
            user = new AppUser
            {
                UserName = phone,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                FirstName = "کاربر",
                LastName = "",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = "خطا در ایجاد حساب کاربری";
                return RedirectToAction(nameof(Login));
            }
            await _userManager.AddToRoleAsync(user, "Customer");
        }

        if (!user.IsActive)
        {
            TempData["Error"] = "حساب شما غیرفعال است. با پشتیبانی تماس بگیرید.";
            return RedirectToAction(nameof(Login));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _signInManager.SignInAsync(user, isPersistent: true);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ============================================================
    // ADMIN: Password Login
    // ============================================================

    [HttpGet]
    public IActionResult AdminLogin(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true &&
            (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> AdminLogin(
        string username, string password, string? returnUrl = null)
    {
        var user = await _userManager.FindByNameAsync(username)
            ?? await _userManager.FindByEmailAsync(username);

        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
        {
            TempData["Error"] = "نام کاربری یا رمز عبور اشتباه است";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        if (!user.IsActive)
        {
            TempData["Error"] = "حساب شما غیرفعال است";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin")
                   || await _userManager.IsInRoleAsync(user, "SuperAdmin");

        if (!isAdmin)
        {
            TempData["Error"] = "شما دسترسی به پنل ادمین ندارید";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _signInManager.SignInAsync(user, isPersistent: false);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    // ============================================================
    // LOGOUT
    // ============================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}