//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using EShop.Domain.Entities;
//using EShop.Domain.Enums;
//using EShop.Domain.Security;

//namespace EShop.Application.Data;

///// <summary>
///// مقداردهی اولیه‌ی دیتابیس — اجرا در startup
///// </summary>
//public static class DbInitializer
//{
//    public static async Task InitializeAsync(IServiceProvider services)
//    {
//        using var scope = services.CreateScope();
//        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
//        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
//        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

//        try
//        {
//            // اجرای Migration
//            await db.Database.MigrateAsync();

//            // 1. Seed Roles
//            foreach (var roleName in new[] { "SuperAdmin", "Admin", "Customer" })
//            {
//                if (!await roleManager.RoleExistsAsync(roleName))
//                    await roleManager.CreateAsync(new AppRole(roleName)
//                    {
//                        Description = roleName switch
//                        {
//                            "SuperAdmin" => "مدیر ارشد سیستم",
//                            "Admin" => "مدیر فروشگاه",
//                            "Customer" => "مشتری",
//                            _ => ""
//                        }
//                    });
//            }

//            // 2. Seed Permissions
//            var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync();
//            var existingSet = existingCodes.ToHashSet();
//            foreach (var (code, name, group, order) in PermissionCatalog.All)
//            {
//                if (existingSet.Contains(code)) continue;
//                db.Permissions.Add(new Permission
//                {
//                    Code = code,
//                    Name = name,
//                    Group = group,
//                    DisplayOrder = order
//                });
//            }
//            await db.SaveChangesAsync();

//            // 3. SuperAdmin user
//            const string adminPhone = "09120000000";
//            if (await userManager.FindByNameAsync(adminPhone) == null)
//            {
//                var admin = new AppUser
//                {
//                    UserName = adminPhone,
//                    PhoneNumber = adminPhone,
//                    PhoneNumberConfirmed = true,
//                    FirstName = "مدیر",
//                    LastName = "سیستم",
//                    Email = "admin@eshop.local",
//                    EmailConfirmed = true,
//                    IsActive = true,
//                    RegisteredAt = DateTime.UtcNow
//                };

//                var result = await userManager.CreateAsync(admin, "Admin@123456");
//                if (result.Succeeded)
//                {
//                    await userManager.AddToRoleAsync(admin, "SuperAdmin");
//                    logger.LogInformation("SuperAdmin created: {Phone}", adminPhone);
//                }
//                else
//                {
//                    logger.LogWarning("Failed to create SuperAdmin: {Errors}",
//                        string.Join(",", result.Errors.Select(e => e.Description)));
//                }
//            }

//            // 4. Default settings
//            if (!await db.SiteSettings.AnyAsync())
//            {
//                db.SiteSettings.AddRange(
//                    new SiteSetting { Key = "site.name", Value = "فروشگاه ستارخان" },
//                    new SiteSetting { Key = "site.tagline", Value = "خرید آنلاین با بهترین قیمت" },
//                    new SiteSetting { Key = "site.currency", Value = "تومان" },
//                    new SiteSetting { Key = "site.timezone", Value = "Asia/Tehran" },
//                    new SiteSetting { Key = "shipping.free_threshold", Value = "2000000" },
//                    new SiteSetting { Key = "shipping.normal_cost", Value = "50000" },
//                    new SiteSetting { Key = "shipping.express_cost", Value = "120000" },
//                    new SiteSetting { Key = "shipping.pickup_cost", Value = "0" },
//                    new SiteSetting { Key = "shipping.normal_days", Value = "۳-۵ روز کاری" },
//                    new SiteSetting { Key = "shipping.express_cities", Value = "تهران,کرج,اصفهان,مشهد,شیراز" },
//                    new SiteSetting { Key = "payment.zarinpal.sandbox", Value = "true" },
//                    new SiteSetting { Key = "payment.cod.enabled", Value = "true" },
//                    new SiteSetting { Key = "payment.online.enabled", Value = "true" },
//                    new SiteSetting { Key = "payment.cod.fee", Value = "0" },
//                    new SiteSetting { Key = "seo.default_title", Value = "فروشگاه ستارخان | خرید آنلاین" },
//                    new SiteSetting { Key = "seo.default_description", Value = "خرید آنلاین با بهترین قیمت" },
//                    new SiteSetting { Key = "seo.robots_enabled", Value = "true" },
//                    new SiteSetting { Key = "branding.logo_width", Value = "144" },
//                    new SiteSetting { Key = "branding.logo_height", Value = "45" }
//                );
//                await db.SaveChangesAsync();
//                logger.LogInformation("Default site settings seeded");
//            }

//            // 5. Default attributes
//            if (!await db.ProductAttributes.AnyAsync())
//            {
//                var colorAttr = new ProductAttribute
//                {
//                    Name = "رنگ",
//                    Slug = "color",
//                    Type = AttributeType.Color,
//                    DisplayOrder = 1,
//                    IsFilterable = true
//                };
//                var sizeAttr = new ProductAttribute
//                {
//                    Name = "سایز",
//                    Slug = "size",
//                    Type = AttributeType.Select,
//                    DisplayOrder = 2,
//                    IsFilterable = true
//                };
//                var materialAttr = new ProductAttribute
//                {
//                    Name = "جنس",
//                    Slug = "material",
//                    Type = AttributeType.Select,
//                    DisplayOrder = 3,
//                    IsFilterable = false
//                };

//                db.ProductAttributes.AddRange(colorAttr, sizeAttr, materialAttr);
//                await db.SaveChangesAsync();

//                db.ProductAttributeOptions.AddRange(
//                    new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "قرمز", ColorCode = "#e74c3c", DisplayOrder = 1 },
//                    new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "آبی", ColorCode = "#3498db", DisplayOrder = 2 },
//                    new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "سبز", ColorCode = "#2ecc71", DisplayOrder = 3 },
//                    new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "مشکی", ColorCode = "#000000", DisplayOrder = 4 },
//                    new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "سفید", ColorCode = "#ffffff", DisplayOrder = 5 },

//                    new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "S", DisplayOrder = 1 },
//                    new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "M", DisplayOrder = 2 },
//                    new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "L", DisplayOrder = 3 },
//                    new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "XL", DisplayOrder = 4 },
//                    new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "XXL", DisplayOrder = 5 },

//                    new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "چرم", DisplayOrder = 1 },
//                    new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "پارچه", DisplayOrder = 2 },
//                    new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "کتان", DisplayOrder = 3 }
//                );
//                await db.SaveChangesAsync();
//                logger.LogInformation("Default attributes and options seeded");
//            }

//            // 6. Default categories
//            if (!await db.Categories.AnyAsync())
//            {
//                var electronics = new Category { Name = "الکترونیک", Slug = "electronics", IsActive = true, ShowInMenu = true, DisplayOrder = 1 };
//                var fashion = new Category { Name = "مد و پوشاک", Slug = "fashion", IsActive = true, ShowInMenu = true, DisplayOrder = 2 };
//                var home = new Category { Name = "خانه و آشپزخانه", Slug = "home", IsActive = true, ShowInMenu = true, DisplayOrder = 3 };

//                db.Categories.AddRange(electronics, fashion, home);
//                await db.SaveChangesAsync();

//                db.Categories.AddRange(
//                    new Category { Name = "موبایل", Slug = "mobile", ParentId = electronics.Id, IsActive = true, DisplayOrder = 1 },
//                    new Category { Name = "لپ‌تاپ", Slug = "laptop", ParentId = electronics.Id, IsActive = true, DisplayOrder = 2 },
//                    new Category { Name = "هدفون", Slug = "headphone", ParentId = electronics.Id, IsActive = true, DisplayOrder = 3 },
//                    new Category { Name = "لباس مردانه", Slug = "mens-clothing", ParentId = fashion.Id, IsActive = true, DisplayOrder = 1 },
//                    new Category { Name = "لباس زنانه", Slug = "womens-clothing", ParentId = fashion.Id, IsActive = true, DisplayOrder = 2 }
//                );
//                await db.SaveChangesAsync();
//                logger.LogInformation("Default categories seeded");
//            }

//            // 7. Default brand
//            if (!await db.Brands.AnyAsync())
//            {
//                db.Brands.Add(new Brand
//                {
//                    Name = "برند عمومی",
//                    Slug = "general-brand",
//                    IsActive = true,
//                    DisplayOrder = 1
//                });
//                await db.SaveChangesAsync();
//            }

//            logger.LogInformation("Database initialization completed");
//        }
//        catch (Exception ex)
//        {
//            logger.LogError(ex, "Database initialization failed");
//            throw;
//        }
//    }
//}