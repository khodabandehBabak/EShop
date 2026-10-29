using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class OrdersController : Controller
{
    private readonly IOrderWorkflowService _workflow;
    private readonly UserManager<AppUser> _userManager;

    public OrdersController(IOrderWorkflowService workflow, UserManager<AppUser> userManager)
    {
        _workflow = workflow; _userManager = userManager;
    }

    [HasPermission(PermissionCatalog.OrdersView)]
    public async Task<IActionResult> Index(
        OrderStatus? status, string? search, DateTime? fromDate, DateTime? toDate,
        PaymentMethod? paymentMethod, int page = 1)
    {
        var filter = new OrderAdminFilterDto
        {
            Status = status,
            Search = search,
            FromDate = fromDate,
            ToDate = toDate,
            PaymentMethod = paymentMethod,
            Page = Math.Max(1, page),
            PageSize = 20
        };

        var (items, total) = await _workflow.GetForAdminAsync(filter);

        ViewBag.Filter = filter;
        ViewBag.Total = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)filter.PageSize);
        ViewBag.PendingCount = await _workflow.GetPendingCountAsync();

        return View(items);
    }

    [HasPermission(PermissionCatalog.OrdersView)]
    public async Task<IActionResult> Detail(int id)
    {
        var dto = await _workflow.GetAdminDetailAsync(id);
        if (dto == null) return NotFound();
        return View(dto);
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.OrdersProcess)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProcessing(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var (ok, msg) = await _workflow.StartProcessingAsync(id, user!.Id);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.OrdersPack)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkPacked(int id, string? note)
    {
        var user = await _userManager.GetUserAsync(User);
        var (ok, msg) = await _workflow.MarkAsPackedAsync(id, user!.Id, note);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.OrdersShip)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ship(ShipOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join("، ", errors) });
        }
        var user = await _userManager.GetUserAsync(User);
        var (ok, msg) = await _workflow.MarkAsShippedAsync(dto, user!.Id);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.OrdersDeliver)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var (ok, msg) = await _workflow.MarkAsDeliveredAsync(id, user!.Id);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.OrdersCancel)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(CancelOrderDto dto)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "دلیل لغو را وارد کنید" });
        var user = await _userManager.GetUserAsync(User);
        var (ok, msg) = await _workflow.CancelAsync(dto, user!.Id);
        return Json(new { success = ok, message = msg });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.OrdersPack)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkPack([FromForm] List<int> ids)
    {
        if (ids == null || !ids.Any())
            return Json(new { success = false, message = "سفارشی انتخاب نشده" });
        var user = await _userManager.GetUserAsync(User);
        var (ok, fail) = await _workflow.BulkPackAsync(ids, user!.Id);
        return Json(new
        {
            success = ok > 0,
            message = $"{ok} سفارش بسته‌بندی شد" + (fail > 0 ? $" - {fail} ناموفق" : "")
        });
    }

    [HttpPost]
    [HasPermission(PermissionCatalog.OrdersShip)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkShip([FromForm] List<int> ids, string company)
    {
        if (ids == null || !ids.Any() || string.IsNullOrWhiteSpace(company))
            return Json(new { success = false, message = "اطلاعات ناقص" });
        var user = await _userManager.GetUserAsync(User);
        var (ok, fail) = await _workflow.BulkShipAsync(ids, company, user!.Id);
        return Json(new
        {
            success = ok > 0,
            message = $"{ok} سفارش ارسال شد" + (fail > 0 ? $" - {fail} ناموفق" : "")
        });
    }

    [HttpGet]
    [HasPermission(PermissionCatalog.OrdersView)]
    public async Task<IActionResult> PendingCount()
        => Json(await _workflow.GetPendingCountAsync());
}