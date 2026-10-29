using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EShop.Web.Controllers;

[Authorize]
public class PaymentController : Controller
{    
    private readonly ICartService _cart;
    private readonly IOrderService _orders;
    private readonly IPaymentGateway _gateway;
    private readonly IShippingService _shipping;
    private readonly ISettingsService _settings;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        ICartService cart,
        IOrderService orders,
        IPaymentGateway gateway,
        IShippingService shipping,
        ISettingsService settings,
        UserManager<AppUser> userManager,
        ILogger<PaymentController> logger)
    {
        _cart = cart;
        _orders = orders;
        _gateway = gateway;
        _shipping = shipping;
        _settings = settings;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(CheckoutInputDto input)
    {
        var summary = await _cart.GetSummaryAsync(input.ShippingMethod);

        if (!summary.Items.Any())
            return RedirectToAction("Index", "Cart");

        // محاسبه‌ی مجدد هزینه ارسال
        var shippingCost = await _shipping.GetCostAsync(input.ShippingMethod, summary.SubTotal);
        summary.ShippingCost = shippingCost;
        summary.TotalAmount = summary.SubTotal + shippingCost - summary.DiscountAmount;

        ViewBag.Summary = summary;
        ViewBag.CodEnabled = await _settings.GetBoolAsync(
            EShop.Domain.Settings.SettingsKeys.PaymentCodEnabled, true);
        ViewBag.OnlineEnabled = await _settings.GetBoolAsync(
            EShop.Domain.Settings.SettingsKeys.PaymentOnlineEnabled, true);
        ViewBag.CodFee = await _settings.GetDecimalAsync(
            EShop.Domain.Settings.SettingsKeys.PaymentCodFee, 0m);

        if (!ModelState.IsValid)
        {
            //ViewBag.Summary = summary;
            return View("~/Views/Checkout/Index.cshtml", input);
        }

        try
        {
            var order = await _orders.CreateAsync(input, summary);

            // پرداخت درب منزل
            if (input.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                _cart.Clear();
                TempData["Success"] = "سفارش شما ثبت شد. پرداخت درب منزل انجام می‌شود.";
                return RedirectToAction(nameof(Success),
                    new { orderNumber = order.OrderNumber });
            }

            // پرداخت آنلاین → زرین‌پال
            var callbackUrl = Url.Action(
                nameof(Callback), "Payment",
                new { orderNumber = order.OrderNumber },
                protocol: Request.Scheme)!;

            var payReq = new PaymentRequestDto(
                order.OrderNumber,
                order.TotalAmount,
                $"پرداخت سفارش {order.OrderNumber} - فروشگاه ستارخان",
                callbackUrl);

            var result = await _gateway.RequestAsync(payReq);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage
                    ?? "خطا در اتصال به درگاه پرداخت");
                //ViewBag.Summary = summary;
                return View("~/Views/Checkout/Index.cshtml", input);
            }

            TempData[$"auth_{order.OrderNumber}"] = result.Authority;
            TempData.Keep($"auth_{order.OrderNumber}");

            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            //ViewBag.Summary = summary;
            return View("~/Views/Checkout/Index.cshtml", input);
        }
    }

    [HttpGet("/payment/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        string orderNumber, string Authority, string Status)
    {
        _logger.LogInformation(
            "Payment callback: {Order} - Authority={Authority} - Status={Status}",
            orderNumber, Authority, Status);

        var user = await _userManager.GetUserAsync(User);
        var order = await _orders.GetByOrderNumberAsync(orderNumber);
        if (order == null) return NotFound();

        if (order.CustomerId.HasValue && user != null && order.CustomerId != user.Id)
            return Forbid();

        if (Status != "OK")
        {
            await _orders.MarkAsFailedAsync(order.Id, "لغو توسط کاربر");
            ViewBag.OrderNumber = order.OrderNumber;
            ViewBag.Message = "پرداخت لغو شد یا ناموفق بود";
            return View("Failed");
        }

        var savedAuthority = TempData[$"auth_{orderNumber}"] as string;
        if (!string.IsNullOrEmpty(savedAuthority) && savedAuthority != Authority)
        {
            await _orders.MarkAsFailedAsync(order.Id, "Authority نامعتبر");
            ViewBag.OrderNumber = order.OrderNumber;
            ViewBag.Message = "پارامترهای پرداخت معتبر نیست";
            return View("Failed");
        }

        var verify = await _gateway.VerifyAsync(Authority, order.TotalAmount);
        if (!verify.Success)
        {
            await _orders.MarkAsFailedAsync(order.Id, verify.ErrorMessage);
            ViewBag.OrderNumber = order.OrderNumber;
            ViewBag.Message = verify.ErrorMessage ?? "پرداخت تأیید نشد";
            return View("Failed");
        }

        await _orders.MarkAsPaidAsync(order.Id, verify.RefId!);
        _cart.Clear();

        return RedirectToAction(nameof(Success),
            new { orderNumber = order.OrderNumber, refId = verify.RefId });
    }

    [HttpGet("/payment/success")]
    public async Task<IActionResult> Success(string orderNumber, string? refId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var order = await _orders.GetByOrderNumberAsync(orderNumber);
        if (order == null) return NotFound();

        if (order.CustomerId.HasValue && order.CustomerId != user?.Id)
            return Forbid();

        ViewBag.OrderNumber = order.OrderNumber;
        ViewBag.RefId = refId ?? order.PaymentTrackingCode;
        ViewBag.TotalAmount = order.TotalAmount;
        ViewBag.PaymentMethod = order.PaymentMethod;

        return View(order);
    }

    [HttpGet("/payment/failed")]
    public IActionResult Failed() => View();
}