using EShop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EShop.Web.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cart;
    public CartController(ICartService cart) => _cart = cart;

    [HttpGet("/cart")]
    public async Task<IActionResult> Index()
    {
        var summary = await _cart.GetSummaryAsync();
        return View(summary);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    //[Authorize]
    public async Task<IActionResult> Add(int productId, int? variantId, int quantity = 1)
    {
        try
        {
            await _cart.AddAsync(productId, variantId, quantity);
            var count = await _cart.GetItemCountAsync();
            var summary = await _cart.GetSummaryAsync();
            var item = summary.Items.FirstOrDefault(i =>
                i.ProductId == productId && i.VariantId == variantId);

            return Json(new
            {
                success = true,
                count,
                message = "به سبد خرید اضافه شد",
                item = item == null ? null : new
                {
                    name = item.Name,
                    variantDescription = item.VariantDescription,
                    imageUrl = item.ImageUrl,
                    price = item.Price,
                    quantity = item.Quantity
                },
                cartTotal = summary.SubTotal
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(
        int productId, int? variantId, int quantity)
    {
        await _cart.UpdateQuantityAsync(productId, variantId, quantity);
        var summary = await _cart.GetSummaryAsync();
        return Json(new
        {
            success = true,
            subTotal = summary.SubTotal,
            shippingCost = summary.ShippingCost,
            total = summary.TotalAmount,
            count = summary.ItemCount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId, int? variantId)
    {
        await _cart.RemoveAsync(productId, variantId);
        var summary = await _cart.GetSummaryAsync();
        return Json(new
        {
            success = true,
            subTotal = summary.SubTotal,
            total = summary.TotalAmount,
            count = summary.ItemCount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cart.Clear();
        return RedirectToAction(nameof(Index));
    }
}