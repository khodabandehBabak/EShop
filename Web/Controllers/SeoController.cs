using EShop.Application.Data;
using EShop.Application.Interfaces;
using EShop.Domain.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml.Linq;

namespace EShop.Web.Controllers;

public class SeoController : Controller
{
    private readonly IApplicationDbContext _db;
    private readonly ISettingsService _settings;

    public SeoController(
        IApplicationDbContext db,
        ISettingsService settings)
    {
        _db = db;
        _settings = settings;
    }

    [HttpGet("/sitemap.xml")]
    [Produces("application/xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var products = await _db.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Slug, p.UpdatedAt, p.CreatedAt })
            .ToListAsync();

        var categories = await _db.Categories
            .Where(c => c.IsActive)
            .Select(c => new { c.Slug, c.UpdatedAt })
            .ToListAsync();

        var brands = await _db.Brands
            .Where(b => b.IsActive)
            .Select(b => new { b.Slug, b.UpdatedAt })
            .ToListAsync();

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var urlset = new XElement(ns + "urlset",
            new XElement(ns + "url",
                new XElement(ns + "loc", baseUrl),
                new XElement(ns + "changefreq", "daily"),
                new XElement(ns + "priority", "1.0")),
            new XElement(ns + "url",
                new XElement(ns + "loc", $"{baseUrl}/shop"),
                new XElement(ns + "changefreq", "daily"),
                new XElement(ns + "priority", "0.9")),
            products.Select(p =>
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/product/{p.Slug}"),
                    new XElement(ns + "lastmod",
                        (p.UpdatedAt ?? p.CreatedAt).ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.8"))),
            categories.Select(c =>
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/category/{c.Slug}"),
                    new XElement(ns + "lastmod",
                        (c.UpdatedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.7"))),
            brands.Select(b =>
                new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/brand/{b.Slug}"),
                    new XElement(ns + "lastmod",
                        (b.UpdatedAt ?? DateTime.UtcNow).ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.6")))
        );

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
        var xml = doc.ToString();

        return Content(xml, "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [Produces("text/plain")]
    public async Task<IActionResult> Robots()
    {
        var robotsEnabled = await _settings.GetBoolAsync(
            SettingsKeys.SeoRobotsEnabled, true);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        if (!robotsEnabled)
        {
            return Content("User-agent: *\nDisallow: /\n", "text/plain", Encoding.UTF8);
        }

        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /admin/");
        sb.AppendLine("Disallow: /customer/");
        sb.AppendLine("Disallow: /cart");
        sb.AppendLine("Disallow: /checkout");
        sb.AppendLine("Disallow: /payment/");
        sb.AppendLine("Disallow: /review/");
        sb.AppendLine("Disallow: /account/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}