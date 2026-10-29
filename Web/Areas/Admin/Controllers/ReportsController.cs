using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ReportsController : Controller
{
    private readonly ISalesReportService _reports;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ISalesReportService reports, ILogger<ReportsController> logger)
    {
        _reports = reports; _logger = logger;
    }

    [HasPermission(PermissionCatalog.ReportsView)]
    public async Task<IActionResult> Index(
        ReportPeriod period = ReportPeriod.Last30Days,
        DateTime? from = null, DateTime? to = null)
    {
        var filter = new SalesReportFilterDto
        {
            Period = period,
            CustomFrom = from,
            CustomTo = to
        };

        try
        {
            var report = await _reports.GenerateAsync(filter);
            return View(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate sales report");
            TempData["Error"] = "خطا در تولید گزارش: " + ex.Message;
            return View(new SalesReportDto { Filter = filter });
        }
    }
}