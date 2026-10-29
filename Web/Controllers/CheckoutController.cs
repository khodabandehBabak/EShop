using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ICartService _cart;
    private readonly IShippingService _shipping;
    private readonly ISettingsService _settings;

    public CheckoutController(
        ICartService cart, IShippingService shipping, ISettingsService settings)
    {
        _cart = cart;
        _shipping = shipping;
        _settings = settings;
    }

    [HttpGet("/checkout")]
    public async Task<IActionResult> Index()
    {
        var summary = await _cart.GetSummaryAsync();
        if (!summary.Items.Any())
            return RedirectToAction("Index", "Cart");

        ViewBag.Summary = summary;
        ViewBag.CodEnabled = await _settings.GetBoolAsync(
            Domain.Settings.SettingsKeys.PaymentCodEnabled, true);
        ViewBag.OnlineEnabled = await _settings.GetBoolAsync(
            Domain.Settings.SettingsKeys.PaymentOnlineEnabled, true);
        ViewBag.CodFee = await _settings.GetDecimalAsync(
            Domain.Settings.SettingsKeys.PaymentCodFee, 0m);

        return View(new CheckoutInputDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetShippingCost(ShippingMethod method)
    {
        var summary = await _cart.GetSummaryAsync(method);
        var cost = await _shipping.GetCostAsync(method, summary.SubTotal);
        return Json(new
        {
            success = true,
            shippingCost = cost,
            subTotal = summary.SubTotal,
            total = summary.SubTotal + cost - summary.DiscountAmount
        });
    }
}