using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EShop.Application.Interfaces;
using EShop.Domain.Security;
using EShop.Infrastructure.Authorization;

namespace EShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class PermissionsController : Controller
{
    private readonly IPermissionService _perms;
    public PermissionsController(IPermissionService perms) => _perms = perms;

    [HasPermission(PermissionCatalog.PermissionsView)]
    public async Task<IActionResult> Index()
        => View(await _perms.GetAllGroupedAsync());
}