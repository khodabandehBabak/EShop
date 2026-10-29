using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;

namespace EShop.Web.Controllers;

public class ProductController : Controller
{
    private readonly IProductStorefrontService _products;
    private readonly IProductViewTracker _viewTracker;
    private readonly IReviewService _reviews;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        IProductStorefrontService products,
        IProductViewTracker viewTracker,
        IReviewService reviews,
        UserManager<AppUser> userManager,
        ILogger<ProductController> logger)
    {
        _products = products;
        _viewTracker = viewTracker;
        _reviews = reviews;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet("/product/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var product = await _products.GetBySlugAsync(slug);
        if (product == null) return NotFound();

        // ثبت بازدید (fire-and-forget)
        _ = _viewTracker.TrackAsync(product.Id);

        ViewBag.RelatedProducts = await _products.GetRelatedAsync(
            product.Id, product.CategoryId);

        // نظرات
        int? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            userId = user?.Id;
        }

        ViewData["ReviewsPage"] = await _reviews.GetForProductAsync(
            product.Id, page: 1, pageSize: 10, currentUserId: userId);

        ViewData["Title"] = product.MetaTitle ?? product.Name;
        ViewData["Description"] = product.MetaDescription
            ?? product.ShortDescription
            ?? $"خرید {product.Name} با بهترین قیمت از فروشگاه ستارخان";

        return View(product);
    }
}