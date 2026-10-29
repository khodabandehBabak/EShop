using Microsoft.AspNetCore.Mvc;
using EShop.Application.Interfaces;

namespace EShop.Web.Controllers;

public class HomeController : Controller
{
    private readonly IHomeService _home;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IHomeService home, ILogger<HomeController> logger)
    {
        _home = home;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var vm = await _home.GetHomeDataAsync();
            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading home page");
            return View(new Application.DTOs.HomeViewModel());
        }
    }

    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Error() => View();
}