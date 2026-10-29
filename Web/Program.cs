using EShop.Application.Data;
using EShop.Application.Interfaces;
using EShop.Application.Services;
using EShop.Domain.Entities;
using EShop.Infrastructure.Authorization;
using EShop.Infrastructure.Data;
using EShop.Infrastructure.Identity;
using EShop.Infrastructure.Services;

//using EShop.Infrastructure.Services;
using EShop.Web.BackgroundServices;
using EShop.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
//using Web.BackgroundServices;
//using Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. Serilog
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/eshop-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ============================================================
// 2. Database
// ============================================================
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IApplicationDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// ============================================================
// 3. Identity
// ============================================================
builder.Services.AddIdentity<AppUser, AppRole>(opt =>
{
    opt.Password.RequireDigit = true;
    opt.Password.RequireLowercase = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequiredLength = 6;
    opt.User.RequireUniqueEmail = false;
    opt.SignIn.RequireConfirmedPhoneNumber = false;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    opt.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Custom Claims Principal Factory
builder.Services.AddScoped<
    IUserClaimsPrincipalFactory<AppUser>,
    AppUserClaimsPrincipalFactory>();

// Cookie config
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath = "/account/login";
    opt.LogoutPath = "/account/logout";
    opt.AccessDeniedPath = "/account/accessdenied";
    opt.ExpireTimeSpan = TimeSpan.FromDays(7);
    opt.SlidingExpiration = true;
    opt.Cookie.HttpOnly = true;
    opt.Cookie.Name = "EShop.Auth";
    opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    opt.Cookie.SameSite = SameSiteMode.Lax;
});

// ============================================================
// 4. Authorization (Permission-based)
// ============================================================
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("CustomerOnly", p => p
        .RequireAuthenticatedUser()
        .RequireRole("Customer", "Admin", "SuperAdmin"));
});

// ============================================================
// 5. Session + Memory Cache
// ============================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromHours(3);
    opt.Cookie.HttpOnly = true;
    opt.Cookie.IsEssential = true;
    opt.Cookie.Name = "EShop.Session";
});
builder.Services.AddMemoryCache();

// ============================================================
// 6. SignalR
// ============================================================
builder.Services.AddSignalR();

// ============================================================
// 7. MVC + AntiForgery
// ============================================================
builder.Services.AddControllersWithViews(opt =>
{
    opt.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "EShop.CSRF";
});

// ============================================================
// 8. Rate Limiting
// ============================================================
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("otp", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 3;
        o.QueueLimit = 0;
    });
    opt.AddFixedWindowLimiter("reviews", o =>
    {
        o.Window = TimeSpan.FromMinutes(10);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
    });
});

// ============================================================
// 9. HSTS
// ============================================================
builder.Services.AddHsts(opt =>
{
    opt.MaxAge = TimeSpan.FromDays(365);
    opt.IncludeSubDomains = true;
    opt.Preload = true;
});

// ============================================================
// 10. Application Services (DI)
// ============================================================
builder.Services.AddHttpContextAccessor();

// Utility
builder.Services.AddScoped<IFileService, LocalFileService>();
builder.Services.AddScoped<ISlugService, SlugService>();
builder.Services.AddScoped<ISmsService, SmsService>();

// External
builder.Services.AddHttpClient<IPaymentGateway, ZarinpalGateway>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Core
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IShippingService, ShippingService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Catalog
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductStorefrontService, ProductStorefrontService>();
builder.Services.AddScoped<IProductAttributeService, ProductAttributeService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<ISliderService, SliderService>();
builder.Services.AddScoped<IHomeService, HomeService>();

// Identity/Permissions
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

// Workflow
builder.Services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
builder.Services.AddScoped<ISalesReportService, SalesReportService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IProductViewTracker, ProductViewTracker>();

// Background
builder.Services.AddHostedService<ViewCountFlushService>();

// ============================================================
// 11. Upload size limit
// ============================================================
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opt =>
{
    opt.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

var app = builder.Build();

// ============================================================
// 12. Security Headers Middleware
// ============================================================
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";
    ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});

// ============================================================
// 13. Pipeline
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/home/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Routes
app.MapControllerRoute(
    name: "customer-root",
    pattern: "customer",
    defaults: new { area = "Customer", controller = "Dashboard", action = "Index" });

app.MapControllerRoute(
    name: "admin-root",
    pattern: "admin",
    defaults: new { area = "Admin", controller = "Dashboard", action = "Index" });

// routeهای خاص Storefront
app.MapControllerRoute(
    name: "product",
    pattern: "product/{slug}",
    defaults: new { controller = "Product", action = "Detail" });

app.MapControllerRoute(
    name: "category",
    pattern: "category/{slug}",
    defaults: new { controller = "Shop", action = "Category" });

app.MapControllerRoute(
    name: "brand",
    pattern: "brand/{slug}",
    defaults: new { controller = "Shop", action = "Brand" });

// SEO
app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Seo", action = "Sitemap" });

app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Seo", action = "Robots" });

// ✅ حالا route عمومی Areas
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// route پیش‌فرض
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<OrderHub>("/orderHub");

// ============================================================
// 14. DB Initialization
// ============================================================
try
{
    await DbInitializer.InitializeAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database initialization failed");
}

// ============================================================
// 15. Graceful shutdown — flush view counts
// ============================================================
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var tracker = scope.ServiceProvider.GetService<IProductViewTracker>();
    if (tracker != null && tracker.PendingCount > 0)
    {
        try { tracker.FlushAsync().GetAwaiter().GetResult(); }
        catch { /* ignore */ }
    }
});

app.Run();