using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using EShop.Domain.Security;

namespace EShop.Infrastructure.Data;

/// <summary>
/// مقداردهی اولیه‌ی دیتابیس — اجرا در startup
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {       
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // اجرای Migration
            await db.Database.MigrateAsync();

            // 1. Seed Roles
            foreach (var roleName in new[] { "SuperAdmin", "Admin", "Customer" })
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new AppRole(roleName)
                    {
                        Description = roleName switch
                        {
                            "SuperAdmin" => "مدیر ارشد سیستم",
                            "Admin" => "مدیر فروشگاه",
                            "Customer" => "مشتری",
                            _ => ""
                        }
                    });
            }

            // 2. Seed Permissions
            var existingCodes = await db.Permissions.Select(p => p.Code).ToListAsync();
            var existingSet = existingCodes.ToHashSet();
            foreach (var (code, name, group, order) in PermissionCatalog.All)
            {
                if (existingSet.Contains(code)) continue;
                db.Permissions.Add(new Permission
                {
                    Code = code,
                    Name = name,
                    Group = group,
                    DisplayOrder = order
                });
            }
            await db.SaveChangesAsync();

            // 3. SuperAdmin user
            const string adminPhone = "09120000000";
            if (await userManager.FindByNameAsync(adminPhone) == null)
            {
                var admin = new AppUser
                {
                    UserName = adminPhone,
                    PhoneNumber = adminPhone,
                    PhoneNumberConfirmed = true,
                    FirstName = "مدیر",
                    LastName = "سیستم",
                    Email = "admin@eshop.local",
                    EmailConfirmed = true,
                    IsActive = true,
                    RegisteredAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "SuperAdmin");
                    logger.LogInformation("SuperAdmin created: {Phone}", adminPhone);
                }
                else
                {
                    logger.LogWarning("Failed to create SuperAdmin: {Errors}",
                        string.Join(",", result.Errors.Select(e => e.Description)));
                }
            }

            // 4. Default settings
            if (!await db.SiteSettings.AnyAsync())
            {
                db.SiteSettings.AddRange(
                    new SiteSetting { Key = "site.name", Value = "فروشگاه ستارخان" },
                    new SiteSetting { Key = "site.tagline", Value = "خرید آنلاین با بهترین قیمت" },
                    new SiteSetting { Key = "site.currency", Value = "تومان" },
                    new SiteSetting { Key = "site.timezone", Value = "Asia/Tehran" },
                    new SiteSetting { Key = "shipping.free_threshold", Value = "2000000" },
                    new SiteSetting { Key = "shipping.normal_cost", Value = "50000" },
                    new SiteSetting { Key = "shipping.express_cost", Value = "120000" },
                    new SiteSetting { Key = "shipping.pickup_cost", Value = "0" },
                    new SiteSetting { Key = "shipping.normal_days", Value = "۳-۵ روز کاری" },
                    new SiteSetting { Key = "shipping.express_cities", Value = "تهران,کرج,اصفهان,مشهد,شیراز" },
                    new SiteSetting { Key = "payment.zarinpal.sandbox", Value = "true" },
                    new SiteSetting { Key = "payment.cod.enabled", Value = "true" },
                    new SiteSetting { Key = "payment.online.enabled", Value = "true" },
                    new SiteSetting { Key = "payment.cod.fee", Value = "0" },
                    new SiteSetting { Key = "seo.default_title", Value = "فروشگاه ستارخان | خرید آنلاین" },
                    new SiteSetting { Key = "seo.default_description", Value = "خرید آنلاین با بهترین قیمت" },
                    new SiteSetting { Key = "seo.robots_enabled", Value = "true" },
                    new SiteSetting { Key = "branding.logo_width", Value = "144" },
                    new SiteSetting { Key = "branding.logo_height", Value = "45" }
                );
                await db.SaveChangesAsync();
                logger.LogInformation("Default site settings seeded");
            }

            // 5. Default attributes
            //if (!await db.ProductAttributes.AnyAsync())
            //{
            //    var colorAttr = new ProductAttribute
            //    {
            //        Name = "رنگ",
            //        Slug = "color",
            //        Type = AttributeType.Color,
            //        DisplayOrder = 1,
            //        IsFilterable = true
            //    };
            //var sizeAttr = new ProductAttribute
            //{
            //    Name = "سایز",
            //    Slug = "size",
            //    Type = AttributeType.Select,
            //    DisplayOrder = 2,
            //    IsFilterable = true
            //};
            //var materialAttr = new ProductAttribute
            //{
            //    Name = "جنس",
            //    Slug = "material",
            //    Type = AttributeType.Select,
            //    DisplayOrder = 3,
            //    IsFilterable = false
            //};

            //    db.ProductAttributes.AddRange(colorAttr, sizeAttr, materialAttr);
            //    await db.SaveChangesAsync();

            //    db.ProductAttributeOptions.AddRange(
            //        new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "قرمز", ColorCode = "#e74c3c", DisplayOrder = 1 },
            //        new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "آبی", ColorCode = "#3498db", DisplayOrder = 2 },
            //        new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "سبز", ColorCode = "#2ecc71", DisplayOrder = 3 },
            //        new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "مشکی", ColorCode = "#000000", DisplayOrder = 4 },
            //        new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "سفید", ColorCode = "#ffffff", DisplayOrder = 5 },

            //new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "S", DisplayOrder = 1 },
            //        new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "M", DisplayOrder = 2 },
            //        new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "L", DisplayOrder = 3 },
            //        new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "XL", DisplayOrder = 4 },
            //        new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "XXL", DisplayOrder = 5 },

            //        new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "چرم", DisplayOrder = 1 },
            //        new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "پارچه", DisplayOrder = 2 },
            //        new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "کتان", DisplayOrder = 3 }
            //    );
            //    await db.SaveChangesAsync();
            //    logger.LogInformation("Default attributes and options seeded");
            //}

            // 6. Default categories
            //if (!await db.Categories.AnyAsync())
            //{
            //var electronics = new Category { Name = "الکترونیک", Slug = "electronics", IsActive = true, ShowInMenu = true, DisplayOrder = 1 };
            //var fashion = new Category { Name = "مد و پوشاک", Slug = "fashion", IsActive = true, ShowInMenu = true, DisplayOrder = 2 };
            //var home = new Category { Name = "خانه و آشپزخانه", Slug = "home", IsActive = true, ShowInMenu = true, DisplayOrder = 3 };

            //db.Categories.AddRange(electronics, fashion, home);
            //await db.SaveChangesAsync();

            //db.Categories.AddRange(
            //    new Category { Name = "موبایل", Slug = "mobile", ParentId = electronics.Id, IsActive = true, DisplayOrder = 1 },
            //    new Category { Name = "لپ‌تاپ", Slug = "laptop", ParentId = electronics.Id, IsActive = true, DisplayOrder = 2 },
            //    new Category { Name = "هدفون", Slug = "headphone", ParentId = electronics.Id, IsActive = true, DisplayOrder = 3 },
            //    new Category { Name = "لباس مردانه", Slug = "mens-clothing", ParentId = fashion.Id, IsActive = true, DisplayOrder = 1 },
            //    new Category { Name = "لباس زنانه", Slug = "womens-clothing", ParentId = fashion.Id, IsActive = true, DisplayOrder = 2 }
            //);
            //await db.SaveChangesAsync();

            //// 7. Default brand
            //if (!await db.Brands.AnyAsync())
            //{
            //    db.Brands.Add(new Brand
            //    {
            //        Name = "برند عمومی",
            //        Slug = "general-brand",
            //        IsActive = true,
            //        DisplayOrder = 1
            //    });
            //    await db.SaveChangesAsync();
            //}




            // ────────────────────────────────────────────────────────
            // جلوگیری از اجرای مجدد
            // ────────────────────────────────────────────────────────
            if (await db.Categories.AnyAsync()) return;

            var now = DateTime.UtcNow;

            // ════════════════════════════════════════════════════════
            // بخش ۱: دسته‌بندی — سطح ۱ (۱۳ ریشه)
            // ════════════════════════════════════════════════════════
            var basicFood = new Category { Name = "کالای اساسی و خواروبار", Slug = "basic-food", DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var beverages = new Category { Name = "نوشیدنی", Slug = "beverages", DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var snacks = new Category { Name = "تنقلات", Slug = "snacks", DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var dairy = new Category { Name = "لبنیات", Slug = "dairy", DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var cosmetics = new Category { Name = "آرایشی و بهداشتی", Slug = "cosmetics", DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var cannedFood = new Category { Name = "کنسرو و غذای آماده", Slug = "canned-food", DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var homeCleaning = new Category { Name = "نظافت منزل", Slug = "home-cleaning", DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var spices = new Category { Name = "چاشنی و ادویه جات", Slug = "spices", DisplayOrder = 8, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var homeLifestyle = new Category { Name = "خانه و سبک زندگی", Slug = "home-lifestyle", DisplayOrder = 9, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var motherBaby = new Category { Name = "مادر و کودک", Slug = "mother-baby", DisplayOrder = 10, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var breakfastCat = new Category { Name = "صبحانه", Slug = "breakfast", DisplayOrder = 11, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var fruitsVegetables = new Category { Name = "میوه و سبزیجات", Slug = "fruits-vegetables", DisplayOrder = 12, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var proteinFoods = new Category { Name = "مواد پروتئینی", Slug = "protein-foods", DisplayOrder = 13, IsActive = true, ShowInMenu = true, CreatedAt = now };

            db.Categories.AddRange(basicFood, beverages, snacks, dairy, cosmetics, cannedFood,
                homeCleaning, spices, homeLifestyle, motherBaby, breakfastCat,
                fruitsVegetables, proteinFoods);
            await db.SaveChangesAsync();

            // ════════════════════════════════════════════════════════
            // بخش ۲: دسته‌بندی — سطح ۲ (۶۴ زیرشاخه)
            // ════════════════════════════════════════════════════════
            var grainsLegumes = new Category { Name = "غلات و حبوبات", Slug = "grains-legumes", ParentId = basicFood.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var sugarCandy = new Category { Name = "قند و شکر و نبات", Slug = "sugar-candy", ParentId = basicFood.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var pastaNoodles = new Category { Name = "ماکارونی و رشته", Slug = "pasta-noodles", ParentId = basicFood.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var breadCat = new Category { Name = "نان", Slug = "bread", ParentId = basicFood.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var oilCat = new Category { Name = "روغن", Slug = "oil", ParentId = basicFood.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var riceCat = new Category { Name = "برنج", Slug = "rice", ParentId = basicFood.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var flourPowder = new Category { Name = "آرد و پودر خوراکی", Slug = "flour-powder", ParentId = basicFood.Id, DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var stillDrinks = new Category { Name = "نوشیدنی بدون گاز", Slug = "still-drinks", ParentId = beverages.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var dairyDrinks = new Category { Name = "نوشیدنی لبنی", Slug = "dairy-drinks", ParentId = beverages.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var carbonatedDrinks = new Category { Name = "نوشیدنی گازدار", Slug = "carbonated-drinks", ParentId = beverages.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var teaHerbal = new Category { Name = "چای و دمنوش", Slug = "tea-herbal", ParentId = beverages.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var distillates = new Category { Name = "عرقیات", Slug = "distillates", ParentId = beverages.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var dessertJelly = new Category { Name = "دسر و ژله", Slug = "dessert-jelly", ParentId = snacks.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var nutsDriedFruits = new Category { Name = "آجیل و خشکبار", Slug = "nuts-dried-fruits", ParentId = snacks.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var gummyJelly = new Category { Name = "پاستیل و ژله آماده", Slug = "gummy-jelly", ParentId = snacks.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var gumBreath = new Category { Name = "آدامس و خوشبوکننده دهان", Slug = "gum-breath", ParentId = snacks.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var snackChips = new Category { Name = "اسنک و چیپس", Slug = "snack-chips", ParentId = snacks.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var biscuitWafer = new Category { Name = "بیسکویت و ویفر", Slug = "biscuit-wafer", ParentId = snacks.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var sweetsChocolate = new Category { Name = "شیرینی و شکلات", Slug = "sweets-chocolate", ParentId = snacks.Id, DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var cakeCookie = new Category { Name = "کیک و کلوچه", Slug = "cake-cookie", ParentId = snacks.Id, DisplayOrder = 8, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var lavashakTamar = new Category { Name = "لواشک و تمر", Slug = "lavashak-tamar", ParentId = snacks.Id, DisplayOrder = 9, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var candyToffee = new Category { Name = "آبنبات و تافی", Slug = "candy-toffee", ParentId = snacks.Id, DisplayOrder = 10, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var breakfastCheese = new Category { Name = "پنیر صبحانه", Slug = "breakfast-cheese", ParentId = dairy.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var milkCat = new Category { Name = "شیر", Slug = "milk", ParentId = dairy.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var butterCat = new Category { Name = "کره", Slug = "butter", ParentId = dairy.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var dryKashk = new Category { Name = "کشک پایه خشک", Slug = "dry-kashk", ParentId = dairy.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var dairyKashk = new Category { Name = "کشک پایه لبنی", Slug = "dairy-kashk", ParentId = dairy.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var iceCream = new Category { Name = "بستنی", Slug = "ice-cream", ParentId = dairy.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var yogurtCat = new Category { Name = "ماست", Slug = "yogurt", ParentId = dairy.Id, DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var cookingCheese = new Category { Name = "پنیر پخت و پز", Slug = "cooking-cheese", ParentId = dairy.Id, DisplayOrder = 8, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var creamCat = new Category { Name = "خامه", Slug = "cream", ParentId = dairy.Id, DisplayOrder = 9, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var personalCare = new Category { Name = "بهداشت فردی", Slug = "personal-care", ParentId = cosmetics.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var cellulosic = new Category { Name = "سلولزی", Slug = "cellulosic", ParentId = cosmetics.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var makeupCat = new Category { Name = "آرایشی", Slug = "makeup", ParentId = cosmetics.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var readyCanned = new Category { Name = "غذای آماده و کنسروجات", Slug = "ready-canned", ParentId = cannedFood.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var coldReadyFood = new Category { Name = "غذای آماده و نیمه آماده سرد", Slug = "cold-ready-food", ParentId = cannedFood.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var textileCleaner = new Category { Name = "شوینده منسوجات", Slug = "textile-cleaner", ParentId = homeCleaning.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var homeHygiene = new Category { Name = "بهداشت خانگی", Slug = "home-hygiene", ParentId = homeCleaning.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var dishCleaner = new Category { Name = "شوینده ظروف", Slug = "dish-cleaner", ParentId = homeCleaning.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var spicesAdditives = new Category { Name = "ادویه و افزودنی ها", Slug = "spices-additives", ParentId = spices.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var picklesCat = new Category { Name = "ترشیجات و شور", Slug = "pickles", ParentId = spices.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var condiments = new Category { Name = "چاشنی", Slug = "condiments", ParentId = spices.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var petsCat = new Category { Name = "حیوانات", Slug = "pets", ParentId = homeLifestyle.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var carAccessories = new Category { Name = "لوازم خودرو", Slug = "car-accessories", ParentId = homeLifestyle.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var homeTools = new Category { Name = "لوازم منزل و ابزار", Slug = "home-tools", ParentId = homeLifestyle.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var clothingAndCare = new Category { Name = "پوشاک و مراقبت از پوشاک", Slug = "clothing-and-care", ParentId = homeLifestyle.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var medicalCat = new Category { Name = "دارویی و طبی", Slug = "medical", ParentId = homeLifestyle.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var electricalComputer = new Category { Name = "لوازم الکتریکی و کامپیوتر", Slug = "electrical-computer", ParentId = homeLifestyle.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var toysKids = new Category { Name = "اسباب بازی و لوازم کودک", Slug = "toys-kids", ParentId = motherBaby.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var entertainment = new Category { Name = "سرگرمی", Slug = "entertainment", ParentId = motherBaby.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var breakfastProducts = new Category { Name = "محصولات صبحانه", Slug = "breakfast-products", ParentId = breakfastCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var summerCrops = new Category { Name = "صیفی جات", Slug = "summer-crops", ParentId = fruitsVegetables.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var freshFruitsVeg = new Category { Name = "میوه و سبزیجات تازه", Slug = "fresh-fruits-veg", ParentId = fruitsVegetables.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };

            var fishShrimp = new Category { Name = "ماهی و میگو", Slug = "fish-shrimp", ParentId = proteinFoods.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var vealOffal = new Category { Name = "آلایش گوساله", Slug = "veal-offal", ParentId = proteinFoods.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var otherPoultry = new Category { Name = "گوشت سایر پرندگان", Slug = "other-poultry", ParentId = proteinFoods.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var beefVeal = new Category { Name = "گوشت گاو و گوساله", Slug = "beef-veal", ParentId = proteinFoods.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var lambCat = new Category { Name = "گوشت گوسفند", Slug = "lamb", ParentId = proteinFoods.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var lambOffal = new Category { Name = "آلایش گوسفند", Slug = "lamb-offal", ParentId = proteinFoods.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var chickenCat = new Category { Name = "گوشت مرغ", Slug = "chicken", ParentId = proteinFoods.Id, DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var chickenOffal = new Category { Name = "آلایش مرغ", Slug = "chicken-offal", ParentId = proteinFoods.Id, DisplayOrder = 8, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var sausageColdCuts = new Category { Name = "سوسیس و کالباس", Slug = "sausage-cold-cuts", ParentId = proteinFoods.Id, DisplayOrder = 9, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var eggsCat = new Category { Name = "تخم پرندگان", Slug = "eggs", ParentId = proteinFoods.Id, DisplayOrder = 10, IsActive = true, ShowInMenu = true, CreatedAt = now };
            var groundMeat = new Category { Name = "گوشت چرخ کرده", Slug = "ground-meat", ParentId = proteinFoods.Id, DisplayOrder = 11, IsActive = true, ShowInMenu = true, CreatedAt = now };

            db.Categories.AddRange(
                grainsLegumes, sugarCandy, pastaNoodles, breadCat, oilCat, riceCat, flourPowder,
                stillDrinks, dairyDrinks, carbonatedDrinks, teaHerbal, distillates,
                dessertJelly, nutsDriedFruits, gummyJelly, gumBreath, snackChips, biscuitWafer,
                sweetsChocolate, cakeCookie, lavashakTamar, candyToffee,
                breakfastCheese, milkCat, butterCat, dryKashk, dairyKashk, iceCream, yogurtCat, cookingCheese, creamCat,
                personalCare, cellulosic, makeupCat,
                readyCanned, coldReadyFood,
                textileCleaner, homeHygiene, dishCleaner,
                spicesAdditives, picklesCat, condiments,
                petsCat, carAccessories, homeTools, clothingAndCare, medicalCat, electricalComputer,
                toysKids, entertainment, breakfastProducts,
                summerCrops, freshFruitsVeg,
                fishShrimp, vealOffal, otherPoultry, beefVeal, lambCat, lambOffal,
                chickenCat, chickenOffal, sausageColdCuts, eggsCat, groundMeat);
            await db.SaveChangesAsync();

            // ════════════════════════════════════════════════════════
            // بخش ۳: دسته‌بندی — سطح ۳ (۱۴۶ زیرشاخه)
            // ════════════════════════════════════════════════════════
            db.Categories.AddRange(
                new Category { Name = "غلات", Slug = "grains", ParentId = grainsLegumes.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "حبوبات", Slug = "legumes", ParentId = grainsLegumes.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "قند و شکر", Slug = "sugar", ParentId = sugarCandy.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نقل و نبات", Slug = "rock-candy", ParentId = sugarCandy.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ماکارونی و اسپاگتی", Slug = "macaroni-spaghetti", ParentId = pastaNoodles.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پاستا و لازانیا", Slug = "pasta-lasagna", ParentId = pastaNoodles.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "رشته", Slug = "noodles", ParentId = pastaNoodles.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نان حجیم", Slug = "bulky-bread", ParentId = breadCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نان مسطح", Slug = "flat-bread", ParentId = breadCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نان نیمه حجیم", Slug = "semi-bulky-bread", ParentId = breadCat.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "روغن جامد و نیمه جامد", Slug = "solid-oil", ParentId = oilCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "روغن مایع", Slug = "liquid-oil", ParentId = oilCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "برنج ایرانی", Slug = "iranian-rice", ParentId = riceCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "برنج خارجی", Slug = "foreign-rice", ParentId = riceCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "آرد", Slug = "flour", ParentId = flourPowder.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پودر کیک و شیرینی", Slug = "cake-powder", ParentId = flourPowder.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پودر دسر و ژله", Slug = "dessert-powder", ParentId = flourPowder.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پودر آشپزی", Slug = "cooking-powder", ParentId = flourPowder.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پودر غذای نیمه آماده", Slug = "semi-ready-powder", ParentId = flourPowder.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پودر نوشیدنی", Slug = "drink-powder", ParentId = flourPowder.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "آب", Slug = "water", ParentId = stillDrinks.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "آبمیوه", Slug = "juice", ParentId = stillDrinks.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شربت", Slug = "syrup", ParentId = stillDrinks.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "دوغ", Slug = "dough-drink", ParentId = dairyDrinks.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نوشابه", Slug = "soda", ParentId = carbonatedDrinks.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نوشابه انرژی زا", Slug = "energy-drink", ParentId = carbonatedDrinks.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ماءالشعیر", Slug = "malt-drink", ParentId = carbonatedDrinks.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "آبمیوه گازدار", Slug = "carbonated-juice", ParentId = carbonatedDrinks.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "سودا", Slug = "club-soda", ParentId = carbonatedDrinks.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "چای سیاه ایرانی", Slug = "iranian-black-tea", ParentId = teaHerbal.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "چای سیاه خارجی", Slug = "foreign-black-tea", ParentId = teaHerbal.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "چای سبز", Slug = "green-tea", ParentId = teaHerbal.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "دمنوش", Slug = "herbal-tea", ParentId = teaHerbal.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "عرقیات گیاهی", Slug = "herbal-distillates", ParentId = distillates.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "دسر", Slug = "dessert", ParentId = dessertJelly.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "آجیل", Slug = "nuts", ParentId = nutsDriedFruits.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "خشکبار", Slug = "dried-fruits", ParentId = nutsDriedFruits.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پاستیل", Slug = "gummy", ParentId = gummyJelly.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ژله آماده", Slug = "ready-jelly", ParentId = gummyJelly.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "آدامس", Slug = "chewing-gum", ParentId = gumBreath.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "قرص خوشبوکننده دهان", Slug = "breath-mint", ParentId = gumBreath.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "اسنک و کرانچی", Slug = "snack-crunchy", ParentId = snackChips.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "چیپس", Slug = "chips", ParentId = snackChips.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پاپ کورن", Slug = "popcorn", ParentId = snackChips.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بیسکویت گندم", Slug = "wheat-biscuit", ParentId = biscuitWafer.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بیسکویت جو", Slug = "barley-biscuit", ParentId = biscuitWafer.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بیسکویت چند غله", Slug = "multi-grain-biscuit", ParentId = biscuitWafer.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ویفر ساده", Slug = "plain-wafer", ParentId = biscuitWafer.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ویفر روکش دار", Slug = "coated-wafer", ParentId = biscuitWafer.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کراکر و چوب شور", Slug = "cracker-pretzel", ParentId = biscuitWafer.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شکلات", Slug = "chocolate", ParentId = sweetsChocolate.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیرینی", Slug = "pastry", ParentId = sweetsChocolate.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پشمک", Slug = "pashmak", ParentId = sweetsChocolate.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نی شیر", Slug = "straw-milk", ParentId = sweetsChocolate.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بارهای تنقلاتی", Slug = "snack-bars", ParentId = sweetsChocolate.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کیک", Slug = "cake", ParentId = cakeCookie.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کلوچه", Slug = "kolucheh", ParentId = cakeCookie.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کوکی", Slug = "cookie", ParentId = cakeCookie.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کروسان", Slug = "croissant", ParentId = cakeCookie.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پیراشکی و دونات", Slug = "pirozhki-donut", ParentId = cakeCookie.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "لواشک", Slug = "lavashak", ParentId = lavashakTamar.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "آلوچه", Slug = "alucheh", ParentId = lavashakTamar.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "تافی", Slug = "toffee", ParentId = candyToffee.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر سفید", Slug = "white-cheese", ParentId = breakfastCheese.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر لاکتیکی", Slug = "lactic-cheese", ParentId = breakfastCheese.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر بلغاری", Slug = "bulgarian-cheese", ParentId = breakfastCheese.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر خامه ای", Slug = "cream-cheese", ParentId = breakfastCheese.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر سنتی", Slug = "traditional-cheese", ParentId = breakfastCheese.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر طعم دار", Slug = "flavored-cheese", ParentId = breakfastCheese.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر لبنه", Slug = "labneh-cheese", ParentId = breakfastCheese.Id, DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر تتراپک", Slug = "tetra-milk", ParentId = milkCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر نایلونی", Slug = "nylon-milk", ParentId = milkCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر بطری", Slug = "bottle-milk", ParentId = milkCat.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر طعم دار", Slug = "flavored-milk", ParentId = milkCat.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر گیاهی", Slug = "plant-milk", ParentId = milkCat.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر طعم دار نایلونی", Slug = "nylon-flavored-milk", ParentId = milkCat.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر طعم دار بطری", Slug = "bottle-flavored-milk", ParentId = milkCat.Id, DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر طعم دار تتراپک", Slug = "tetra-flavored-milk", ParentId = milkCat.Id, DisplayOrder = 8, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شیر طعم دار لیوانی", Slug = "cup-flavored-milk", ParentId = milkCat.Id, DisplayOrder = 9, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کره حیوانی", Slug = "animal-butter", ParentId = butterCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کره گیاهی", Slug = "plant-butter", ParentId = butterCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کره مخلوط", Slug = "mixed-butter", ParentId = butterCat.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کشک پایه خشک پاکتی", Slug = "packet-dry-kashk", ParentId = dryKashk.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کشک پایه خشک شیشه ای", Slug = "glass-dry-kashk", ParentId = dryKashk.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کشک لبنی لیوانی", Slug = "cup-dairy-kashk", ParentId = dairyKashk.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بستنی لیوانی", Slug = "cup-ice-cream", ParentId = iceCream.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بستنی قیفی", Slug = "cone-ice-cream", ParentId = iceCream.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بستنی چوبی", Slug = "stick-ice-cream", ParentId = iceCream.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بستنی نونی و سنتی", Slug = "traditional-ice-cream", ParentId = iceCream.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بستنی بار", Slug = "bar-ice-cream", ParentId = iceCream.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بستنی لیتری", Slug = "liter-ice-cream", ParentId = iceCream.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بستنی یخی", Slug = "ice-pop", ParentId = iceCream.Id, DisplayOrder = 7, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ماست ست", Slug = "set-yogurt", ParentId = yogurtCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ماست همزده", Slug = "stirred-yogurt", ParentId = yogurtCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ماست چکیده", Slug = "strained-yogurt", ParentId = yogurtCat.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ماست طعم دار", Slug = "flavored-yogurt", ParentId = yogurtCat.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر موزارلا", Slug = "mozzarella", ParentId = cookingCheese.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر ورقه ای", Slug = "sliced-cheese", ParentId = cookingCheese.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر پارمسان", Slug = "parmesan", ParentId = cookingCheese.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنیر پروسس", Slug = "processed-cheese", ParentId = cookingCheese.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "خامه ساده", Slug = "plain-cream", ParentId = creamCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "خامه شکلاتی", Slug = "chocolate-cream", ParentId = creamCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "خامه عسل", Slug = "honey-cream", ParentId = creamCat.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بهداشت دهان و دندان", Slug = "oral-care", ParentId = personalCare.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بهداشت سر و بدن", Slug = "head-body-care", ParentId = personalCare.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بهداشت کودک", Slug = "baby-care", ParentId = personalCare.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "خوشبوکننده بدن", Slug = "body-fragrance", ParentId = personalCare.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "وسایل بهداشتی", Slug = "hygiene-tools", ParentId = personalCare.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "دستمال", Slug = "tissue", ParentId = cellulosic.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پوشک", Slug = "diaper", ParentId = cellulosic.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "نوار بهداشتی", Slug = "sanitary-pad", ParentId = cellulosic.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پنبه و گوش پاک کن", Slug = "cotton-buds", ParentId = cellulosic.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "محافظ پوست", Slug = "skin-care", ParentId = makeupCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "محافظ مو", Slug = "hair-care", ParentId = makeupCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "محافظ ناخن", Slug = "nail-care", ParentId = makeupCat.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "لوازم آرایش", Slug = "makeup-tools", ParentId = makeupCat.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کنسرو حبوبات و سبزیجات", Slug = "canned-veg", ParentId = readyCanned.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کنسرو مرغ و ماهی", Slug = "canned-meat", ParentId = readyCanned.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "رب", Slug = "tomato-paste", ParentId = readyCanned.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "غذای آماده", Slug = "ready-food", ParentId = readyCanned.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "کنسرو میوه", Slug = "canned-fruit", ParentId = readyCanned.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "محصولات پروتئینی خام منجمد", Slug = "frozen-raw-protein", ParentId = coldReadyFood.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "محصولات نیمه آماده منجمد", Slug = "frozen-semi-ready", ParentId = coldReadyFood.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "میوه و سبزیجات منجمد", Slug = "frozen-fruits-veg", ParentId = coldReadyFood.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "صیفی جات خشک و سرخ شده", Slug = "dried-fried-veg", ParentId = coldReadyFood.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "خمیر پخت و پز", Slug = "cooking-dough", ParentId = coldReadyFood.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "غذای آماده سرد", Slug = "cold-ready-meal", ParentId = coldReadyFood.Id, DisplayOrder = 6, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پودر و قرص لباسشویی", Slug = "laundry-powder", ParentId = textileCleaner.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "مایع شوینده منسوجات", Slug = "laundry-liquid", ParentId = textileCleaner.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "بهداشت منسوجات کودک", Slug = "baby-laundry", ParentId = textileCleaner.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "سفید کننده و ضدعفونی کننده", Slug = "bleach-disinfectant", ParentId = homeHygiene.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "پاک کننده سطوح", Slug = "surface-cleaner", ParentId = homeHygiene.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "حشره کش ها", Slug = "insecticide", ParentId = homeHygiene.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "خوشبو کننده و بوگیر", Slug = "air-freshener", ParentId = homeHygiene.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "لوله بازکن و رسوبگیر", Slug = "drain-cleaner", ParentId = homeHygiene.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "مایعات ظرفشویی", Slug = "dish-liquid", ParentId = dishCleaner.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شوینده ماشین ظرفشویی", Slug = "dishwasher-detergent", ParentId = dishCleaner.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ادویه جات", Slug = "spice-mix", ParentId = spicesAdditives.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "سبزیجات خشک و سرخ شده", Slug = "dried-fried-herbs", ParentId = spicesAdditives.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "گیاه دارویی", Slug = "medicinal-herb", ParentId = spicesAdditives.Id, DisplayOrder = 3, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "دانه گیاهی", Slug = "plant-seed", ParentId = spicesAdditives.Id, DisplayOrder = 4, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "عصاره", Slug = "extract", ParentId = spicesAdditives.Id, DisplayOrder = 5, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "ترشی", Slug = "pickled-veg", ParentId = picklesCat.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "شوریجات", Slug = "salty-pickles", ParentId = picklesCat.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "سس", Slug = "sauce", ParentId = condiments.Id, DisplayOrder = 1, IsActive = true, ShowInMenu = true, CreatedAt = now },
                new Category { Name = "چاشنی های مایع", Slug = "liquid-condiment", ParentId = condiments.Id, DisplayOrder = 2, IsActive = true, ShowInMenu = true, CreatedAt = now }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Database category seed");

            // ════════════════════════════════════════════════════════
            // بخش ۴: categoryMap
            // ════════════════════════════════════════════════════════
            var categoryMap = await db.Categories
                .Where(c => c.Slug != null)
                .ToDictionaryAsync(c => c.Slug!, c => c.Id);

            // ════════════════════════════════════════════════════════
            // بخش ۵: برندها (۲۰ برند)
            // ════════════════════════════════════════════════════════
            db.Brands.AddRange(
                new Brand { Name = "برند عمومی", Slug = "general-brand", Website = null, LogoUrl = null, Description = null, DisplayOrder = 1, IsActive = true, CreatedAt = now },
                new Brand { Name = "کاله", Slug = "kalleh", Website = "https://www.kalleh.com", LogoUrl = "/uploads/brands/kalleh.png", Description = "فراورده‌های لبنی، بستنی، نوشیدنی و محصولات پروتئینی", DisplayOrder = 10, IsActive = true, CreatedAt = now },
                new Brand { Name = "میهن", Slug = "mihan", Website = "https://www.mihan-food.com", LogoUrl = "/uploads/brands/mihan.png", Description = "لبنیات، بستنی و آبمیوه", DisplayOrder = 20, IsActive = true, CreatedAt = now },
                new Brand { Name = "دامداران", Slug = "damdaran", Website = "https://damdaran.ir", LogoUrl = "/uploads/brands/damdaran.png", Description = "لبنیات پاستوریزه و استریل، شیر و ماست", DisplayOrder = 30, IsActive = true, CreatedAt = now },
                new Brand { Name = "خومالی", Slug = "khomali", Website = null, LogoUrl = "/uploads/brands/khomali.png", Description = "دوغ و نوشیدنی‌های لبنی", DisplayOrder = 40, IsActive = true, CreatedAt = now },
                new Brand { Name = "دلستر (بهنوش)", Slug = "delester-behnoush", Website = "https://behnoushiran.com", LogoUrl = "/uploads/brands/delester.png", Description = "ماءالشعیر و نوشیدنی‌های مالت", DisplayOrder = 50, IsActive = true, CreatedAt = now },
                new Brand { Name = "عالیس", Slug = "alis", Website = "https://www.alis.ir", LogoUrl = "/uploads/brands/alis.png", Description = "آبمیوه، نوشیدنی و شیرهای طعم‌دار", DisplayOrder = 60, IsActive = true, CreatedAt = now },
                new Brand { Name = "سن‌ایچ", Slug = "sanich", Website = null, LogoUrl = "/uploads/brands/sanich.png", Description = "آبمیوه و نوشیدنی", DisplayOrder = 70, IsActive = true, CreatedAt = now },
                new Brand { Name = "کوکاکولا", Slug = "coca-cola", Website = "https://www.coca-cola.com", LogoUrl = "/uploads/brands/coca-cola.png", Description = "نوشیدنی‌های گازدار", DisplayOrder = 80, IsActive = true, CreatedAt = now },
                new Brand { Name = "اسپرایت", Slug = "sprite", Website = "https://www.coca-cola.com/sprite", LogoUrl = "/uploads/brands/sprite.png", Description = "نوشیدنی گازدار با طعم لیمو", DisplayOrder = 90, IsActive = true, CreatedAt = now },
                new Brand { Name = "ردبول", Slug = "redbull", Website = "https://www.redbull.com", LogoUrl = "/uploads/brands/redbull.png", Description = "نوشیدنی انرژی‌زا", DisplayOrder = 100, IsActive = true, CreatedAt = now },
                new Brand { Name = "هایپ", Slug = "hype", Website = null, LogoUrl = "/uploads/brands/hype.png", Description = "نوشیدنی انرژی‌زا", DisplayOrder = 110, IsActive = true, CreatedAt = now },
                new Brand { Name = "چی‌توز", Slug = "cheetoz", Website = null, LogoUrl = "/uploads/brands/cheetoz.png", Description = "چیپس، اسنک و تنقلات", DisplayOrder = 120, IsActive = true, CreatedAt = now },
                new Brand { Name = "مینو", Slug = "mino", Website = "https://www.minobrands.com", LogoUrl = "/uploads/brands/mino.png", Description = "بیسکویت، ویفر، شکلات و تنقلات", DisplayOrder = 130, IsActive = true, CreatedAt = now },
                new Brand { Name = "شیرین عسل", Slug = "shirin-asal", Website = "https://www.shirinasal.com", LogoUrl = "/uploads/brands/shirin-asal.png", Description = "بیسکویت، شکلات، ویفر و کیک", DisplayOrder = 140, IsActive = true, CreatedAt = now },
                new Brand { Name = "گلرنگ", Slug = "golrang", Website = "https://golrang.com", LogoUrl = "/uploads/brands/golrang.png", Description = "شوینده، بهداشتی و آرایشی", DisplayOrder = 150, IsActive = true, CreatedAt = now },
                new Brand { Name = "پاکسان", Slug = "paksan", Website = "https://www.paksan.com", LogoUrl = "/uploads/brands/paksan.png", Description = "شوینده، بهداشتی و آرایشی", DisplayOrder = 160, IsActive = true, CreatedAt = now },
                new Brand { Name = "زر ماکارون", Slug = "zar-macaron", Website = "https://www.zarmacaron.com", LogoUrl = "/uploads/brands/zar-macaron.png", Description = "ماکارونی، پاستا و رشته", DisplayOrder = 170, IsActive = true, CreatedAt = now },
                new Brand { Name = "رضا", Slug = "reza", Website = "https://rezafood.com", LogoUrl = "/uploads/brands/reza.png", Description = "ماکارونی، کنسرو و غذای آماده", DisplayOrder = 180, IsActive = true, CreatedAt = now },
                new Brand { Name = "آتوشه", Slug = "atousheh", Website = "https://atousheh.ir", LogoUrl = "/uploads/brands/atousheh.png", Description = "غلات، حبوبات و محصولات خشکبار", DisplayOrder = 190, IsActive = true, CreatedAt = now }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Database brand seed");

            // ════════════════════════════════════════════════════════
            // بخش ۶: brandMap
            // ════════════════════════════════════════════════════════
            var brandMap = await db.Brands
                .Where(b => b.Slug != null)
                .ToDictionaryAsync(b => b.Slug!, b => b.Id);

            // ════════════════════════════════════════════════════════
            // بخش ۷: محصولات (۱۳۰ محصول)
            // ════════════════════════════════════════════════════════
            Product CreateProduct(
                string name, string slug, string categorySlug, decimal price,
                string? shortDescription = null, decimal? discountPrice = null,
                int stock = 50, bool isFeatured = false, string brandSlug = "general-brand")
            {
                if (!categoryMap.TryGetValue(categorySlug, out var catId))
                    throw new InvalidOperationException($"Category '{categorySlug}' not found.");
                if (!brandMap.TryGetValue(brandSlug, out var brId))
                    throw new InvalidOperationException($"Brand '{brandSlug}' not found.");

                return new Product
                {
                    Name = name,
                    Slug = slug,
                    Sku = null,
                    ShortDescription = shortDescription ?? name,
                    FullDescription = shortDescription ?? name,
                    Price = price,
                    DiscountPrice = discountPrice,
                    Stock = stock,
                    LowStockThreshold = 5,
                    Weight = null,
                    Dimensions = null,
                    IsActive = true,
                    IsFeatured = isFeatured,
                    IsSpecialOffer = discountPrice.HasValue,
                    HasVariants = false,
                    ViewCount = Random.Shared.Next(10, 500),
                    LastViewAt = null,
                    MetaTitle = null,
                    MetaDescription = null,
                    MetaKeywords = null,
                    CategoryId = catId,
                    BrandId = brId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    IsDeleted = false
                };
            }

            db.Products.AddRange(
                CreateProduct("بلغور گندم آتوشه ۵۵۰ گرمی", "atiyeh-bulgur-wheat-550g", "grains", 130000m, brandSlug: "atousheh"),
                CreateProduct("جو پرک آتوشه ۵۰۰ گرمی", "atiyeh-barley-flakes-500g", "grains", 95000m, brandSlug: "atousheh"),
                CreateProduct("لوبیا چشم بلبلی آتوشه ۷۰۰ گرمی", "atiyeh-black-eyed-peas-700g", "legumes", 283500m, null, 315000m, 40, false, "atousheh"),
                CreateProduct("نخود آتوشه ۵۰۰ گرمی", "atiyeh-chickpeas-500g", "legumes", 180000m, brandSlug: "atousheh"),
                CreateProduct("قند شکسته ۵ کیلوگرمی", "sugar-cubes-5kg", "sugar", 380000m),
                CreateProduct("شکر سفید ۳ کیلوگرمی", "white-sugar-3kg", "sugar", 250000m),
                CreateProduct("نبات زعفرانی ۵۰۰ گرمی", "saffron-rock-candy-500g", "rock-candy", 120000m),
                CreateProduct("نبات چوبی ۲۰ عددی", "rock-candy-sticks-20pcs", "rock-candy", 80000m),
                CreateProduct("ماکارونی رضا ۷۰۰ گرمی", "reza-macaroni-700g", "macaroni-spaghetti", 48000m, brandSlug: "reza"),
                CreateProduct("اسپاگتی زر ماکارون ۵۰۰ گرمی", "zar-macaron-spaghetti-500g", "macaroni-spaghetti", 42000m, brandSlug: "zar-macaron"),
                CreateProduct("لازانیا زر ماکارون ۵۰۰ گرمی", "zar-macaron-lasagna-500g", "pasta-lasagna", 65000m, brandSlug: "zar-macaron"),
                CreateProduct("پاستا سوپ زر ماکارون ۵۰۰ گرمی", "zar-macaron-soup-pasta-500g", "pasta-lasagna", 55000m, brandSlug: "zar-macaron"),
                CreateProduct("رشته آشی ۵۰۰ گرمی", "vermicelli-soup-500g", "noodles", 35000m),
                CreateProduct("رشته پلویی ۵۰۰ گرمی", "vermicelli-rice-500g", "noodles", 38000m),
                CreateProduct("نان تست حجیم ۴۰۰ گرمی", "bulky-toast-bread-400g", "bulky-bread", 45000m),
                CreateProduct("نان همبرگری حجیم ۶ عددی", "burger-buns-6pcs", "bulky-bread", 38000m),
                CreateProduct("نان لواش ۵۰۰ گرمی", "lavash-bread-500g", "flat-bread", 25000m),
                CreateProduct("نان تافتون ۴۰۰ گرمی", "taftoon-bread-400g", "flat-bread", 28000m),
                CreateProduct("نان بربری ۵۰۰ گرمی", "barbari-bread-500g", "semi-bulky-bread", 32000m),
                CreateProduct("نان شیرمال ۴۰۰ گرمی", "shirmal-bread-400g", "semi-bulky-bread", 40000m),
                CreateProduct("روغن جامد حیوانی ۸۰۰ گرمی", "solid-animal-oil-800g", "solid-oil", 180000m),
                CreateProduct("روغن نباتی جامد ۹۰۰ گرمی", "solid-vegetable-oil-900g", "solid-oil", 150000m),
                CreateProduct("روغن مایع سرخ کردنی ۸۱۰ گرمی", "liquid-frying-oil-810g", "liquid-oil", 120000m),
                CreateProduct("روغن زیتون فرابکر ۵۰۰ میلی‌لیتر", "extra-virgin-olive-oil-500ml", "liquid-oil", 350000m),
                CreateProduct("برنج هاشمی ایرانی ۵ کیلوگرمی", "hashemi-iranian-rice-5kg", "iranian-rice", 1200000m),
                CreateProduct("برنج طارم ایرانی ۵ کیلوگرمی", "tarom-iranian-rice-5kg", "iranian-rice", 1350000m),
                CreateProduct("برنج باسماتی خارجی ۵ کیلوگرمی", "basmati-foreign-rice-5kg", "foreign-rice", 900000m),
                CreateProduct("برنج پاکستانی ۵ کیلوگرمی", "pakistani-rice-5kg", "foreign-rice", 750000m),
                CreateProduct("آرد گندم ۲ کیلوگرمی", "wheat-flour-2kg", "flour", 85000m),
                CreateProduct("آرد برنج ۵۰۰ گرمی", "rice-flour-500g", "flour", 60000m),
                CreateProduct("پودر کیک وانیلی ۵۰۰ گرمی", "vanilla-cake-powder-500g", "cake-powder", 95000m),
                CreateProduct("پودر کیک شکلاتی ۵۰۰ گرمی", "chocolate-cake-powder-500g", "cake-powder", 100000m),
                CreateProduct("پودر ژله توت‌فرنگی ۱۰۰ گرمی", "strawberry-jelly-powder-100g", "dessert-powder", 35000m),
                CreateProduct("پودر دسر شکلاتی ۱۰۰ گرمی", "chocolate-dessert-powder-100g", "dessert-powder", 40000m),
                CreateProduct("پودر سوپ پیاز ۵۰۰ گرمی", "onion-soup-powder-500g", "cooking-powder", 70000m),
                CreateProduct("پودر قورمه سبزی ۲۰۰ گرمی", "ghormeh-sabzi-powder-200g", "cooking-powder", 55000m),
                CreateProduct("پودر کتلت ۲۰۰ گرمی", "kotlet-powder-200g", "semi-ready-powder", 45000m),
                CreateProduct("پودر کوکو ۲۰۰ گرمی", "kuku-powder-200g", "semi-ready-powder", 42000m),
                CreateProduct("پودر شربت آلبالو ۵۰۰ گرمی", "sour-cherry-syrup-powder-500g", "drink-powder", 65000m),
                CreateProduct("پودر شربت پرتقال ۵۰۰ گرمی", "orange-syrup-powder-500g", "drink-powder", 65000m),
                CreateProduct("آب معدنی کوچک ۵۰۰ میلی‌لیتر", "mineral-water-small-500ml", "water", 15000m),
                CreateProduct("آب معدنی بزرگ ۱.۵ لیتری", "mineral-water-large-1.5l", "water", 25000m),
                CreateProduct("آبمیوه ترکیبی بزرگ ۱ لیتری", "mixed-juice-large-1l", "juice", 85000m, brandSlug: "alis"),
                CreateProduct("آبمیوه پرتقال ۱ لیتری", "orange-juice-1l", "juice", 90000m, brandSlug: "alis"),
                CreateProduct("شربت آلبالو ۱ لیتری", "sour-cherry-syrup-1l", "syrup", 180000m),
                CreateProduct("شربت پرتقال ۱ لیتری", "orange-syrup-1l", "syrup", 160000m),
                CreateProduct("دوغ خومالی ۱ لیتری", "khomali-dough-1l", "dough-drink", 150000m, brandSlug: "khomali"),
                CreateProduct("دوغ گازدار ۳۰۰ میلی‌لیتر", "carbonated-dough-300ml", "dough-drink", 45000m),
                CreateProduct("نوشابه کوکاکولا ۱.۵ لیتری", "coca-cola-1.5l", "soda", 85000m, null, null, 50, true, "coca-cola"),
                CreateProduct("نوشابه اسپرایت ۱.۵ لیتری", "sprite-1.5l", "soda", 80000m, null, null, 50, false, "sprite"),
                CreateProduct("نوشابه انرژی زا ردبول ۲۵۰ میلی‌لیتر", "redbull-energy-drink-250ml", "energy-drink", 95000m, brandSlug: "redbull"),
                CreateProduct("نوشابه انرژی زا هایپ ۲۵۰ میلی‌لیتر", "hype-energy-drink-250ml", "energy-drink", 75000m, brandSlug: "hype"),
                CreateProduct("ماءالشعیر دلستر لیمو ۳۳۰ میلی‌لیتر", "delester-malt-lemon-330ml", "malt-drink", 69000m, brandSlug: "delester-behnoush"),
                CreateProduct("ماءالشعیر دلستر کلاسیک ۳۳۰ میلی‌لیتر", "delester-malt-classic-330ml", "malt-drink", 65000m, brandSlug: "delester-behnoush"),
                CreateProduct("آبمیوه گازدار سیب ۲۵۰ میلی‌لیتر", "sparkling-apple-juice-250ml", "carbonated-juice", 40000m, brandSlug: "alis"),
                CreateProduct("آبمیوه گازدار پرتقال ۲۵۰ میلی‌لیتر", "sparkling-orange-juice-250ml", "carbonated-juice", 40000m, brandSlug: "alis"),
                CreateProduct("سودای لیمو ۳۰۰ میلی‌لیتر", "lemon-soda-300ml", "club-soda", 30000m),
                CreateProduct("سودای ساده ۳۰۰ میلی‌لیتر", "plain-soda-300ml", "club-soda", 25000m),
                CreateProduct("چای سیاه ایرانی قلم ۵۰۰ گرمی", "iranian-black-tea-500g", "iranian-black-tea", 350000m),
                CreateProduct("چای سیاه ایرانی شکسته ۵۰۰ گرمی", "iranian-broken-black-tea-500g", "iranian-black-tea", 280000m),
                CreateProduct("چای سیاه خارجی سیلان ۴۰۰ گرمی", "ceylon-black-tea-400g", "foreign-black-tea", 300000m),
                CreateProduct("چای سیاه خارجی آسام ۴۰۰ گرمی", "assam-black-tea-400g", "foreign-black-tea", 320000m),
                CreateProduct("چای سبز کیسه‌ای ۲۵ عددی", "green-tea-bags-25pcs", "green-tea", 120000m),
                CreateProduct("چای سبز برگ ۲۵۰ گرمی", "green-tea-leaf-250g", "green-tea", 180000m),
                CreateProduct("دمنوش به‌لیمو ۲۰ عددی", "quince-lemon-herbal-tea-20pcs", "herbal-tea", 90000m),
                CreateProduct("دمنوش گل گاوزبان ۲۰ عددی", "borage-herbal-tea-20pcs", "herbal-tea", 85000m),
                CreateProduct("عرق نعنا ۱ لیتری", "mint-distillate-1l", "herbal-distillates", 120000m),
                CreateProduct("عرق بیدمشک ۱ لیتری", "bidmeshk-distillate-1l", "herbal-distillates", 150000m),
                CreateProduct("دسر شکلاتی آماده ۱۰۰ گرمی", "ready-chocolate-dessert-100g", "dessert", 45000m, brandSlug: "kalleh"),
                CreateProduct("دسر وانیلی آماده ۱۰۰ گرمی", "ready-vanilla-dessert-100g", "dessert", 40000m, brandSlug: "kalleh"),
                CreateProduct("آجیل مخلوط ۵۰۰ گرمی", "mixed-nuts-500g", "nuts", 450000m),
                CreateProduct("پسته احمدآقایی ۲۵۰ گرمی", "ahmad-aghaei-pistachio-250g", "nuts", 380000m),
                CreateProduct("توت خشک ۲۵۰ گرمی", "dried-mulberry-250g", "dried-fruits", 180000m),
                CreateProduct("انجیر خشک ۲۵۰ گرمی", "dried-fig-250g", "dried-fruits", 220000m),
                CreateProduct("پاستیل میوه‌ای ۱۰۰ گرمی", "fruit-gummy-100g", "gummy", 50000m, brandSlug: "shirin-asal"),
                CreateProduct("پاستیل ترش ۱۰۰ گرمی", "sour-gummy-100g", "gummy", 55000m, brandSlug: "shirin-asal"),
                CreateProduct("ژله آماده توت‌فرنگی ۱۰۰ گرمی", "ready-strawberry-jelly-100g", "ready-jelly", 35000m, brandSlug: "mino"),
                CreateProduct("ژله آماده پرتقال ۱۰۰ گرمی", "ready-orange-jelly-100g", "ready-jelly", 35000m, brandSlug: "mino"),
                CreateProduct("آدامس نعنایی ۲۰ عددی", "mint-chewing-gum-20pcs", "chewing-gum", 45000m),
                CreateProduct("آدامس دارچینی ۲۰ عددی", "cinnamon-chewing-gum-20pcs", "chewing-gum", 45000m),
                CreateProduct("قرص نعنا ۵۰ عددی", "mint-breath-mints-50pcs", "breath-mint", 35000m),
                CreateProduct("قرص دارچین ۵۰ عددی", "cinnamon-breath-mints-50pcs", "breath-mint", 35000m),
                CreateProduct("اسنک کرانچی پنیری ۹۵ گرمی", "cheesy-crunchy-snack-95g", "snack-crunchy", 60000m, brandSlug: "cheetoz"),
                CreateProduct("اسنک کرانچی نمکی ۹۵ گرمی", "salty-crunchy-snack-95g", "snack-crunchy", 55000m, brandSlug: "cheetoz"),
                CreateProduct("چیپس کچاپ چی‌توز ۶۰ گرمی", "cheetoz-ketchup-chips-60g", "chips", 10000m, null, null, 50, true, "cheetoz"),
                CreateProduct("چیپس فلفلی چی‌توز ۶۰ گرمی", "cheetoz-pepper-chips-60g", "chips", 7000m, null, null, 50, false, "cheetoz"),
                CreateProduct("پاپ کورن کره‌ای ۸۰ گرمی", "butter-popcorn-80g", "popcorn", 35000m),
                CreateProduct("پاپ کورن نمکی ۸۰ گرمی", "salty-popcorn-80g", "popcorn", 30000m),
                CreateProduct("بیسکویت گندم ساده ۲۰۰ گرمی", "plain-wheat-biscuit-200g", "wheat-biscuit", 30000m, brandSlug: "shirin-asal"),
                CreateProduct("بیسکویت گندم شکلاتی ۲۰۰ گرمی", "chocolate-wheat-biscuit-200g", "wheat-biscuit", 35000m, brandSlug: "shirin-asal"),
                CreateProduct("بیسکویت جو ساده ۲۰۰ گرمی", "plain-barley-biscuit-200g", "barley-biscuit", 35000m, brandSlug: "mino"),
                CreateProduct("بیسکویت جو عسلی ۲۰۰ گرمی", "honey-barley-biscuit-200g", "barley-biscuit", 38000m, brandSlug: "mino"),
                CreateProduct("بیسکویت چند غله ساده ۲۰۰ گرمی", "plain-multi-grain-biscuit-200g", "multi-grain-biscuit", 40000m),
                CreateProduct("بیسکویت چند غله کنجدی ۲۰۰ گرمی", "sesame-multi-grain-biscuit-200g", "multi-grain-biscuit", 42000m),
                CreateProduct("ویفر ساده وانیلی ۱۰۰ گرمی", "plain-vanilla-wafer-100g", "plain-wafer", 25000m, brandSlug: "mino"),
                CreateProduct("ویفر ساده شکلاتی ۱۰۰ گرمی", "plain-chocolate-wafer-100g", "plain-wafer", 28000m, brandSlug: "mino"),
                CreateProduct("ویفر روکش دار شکلاتی ۵۰ گرمی", "chocolate-coated-wafer-50g", "coated-wafer", 20000m),
                CreateProduct("ویفر روکش دار کاراملی ۵۰ گرمی", "caramel-coated-wafer-50g", "coated-wafer", 22000m),
                CreateProduct("کراکر نمکی ۲۰۰ گرمی", "salty-cracker-200g", "cracker-pretzel", 35000m),
                CreateProduct("چوب شور ۲۰۰ گرمی", "pretzel-sticks-200g", "cracker-pretzel", 30000m),
                CreateProduct("شکلات تخته‌ای تلخ ۱۰۰ گرمی", "dark-chocolate-bar-100g", "chocolate", 85000m),
                CreateProduct("شکلات تخته‌ای شیری ۱۰۰ گرمی", "milk-chocolate-bar-100g", "chocolate", 80000m),
                CreateProduct("شیرینی دانمارکی ۴۰۰ گرمی", "danish-pastry-400g", "pastry", 180000m),
                CreateProduct("شیرینی خشک ۴۰۰ گرمی", "dry-pastry-400g", "pastry", 160000m),
                CreateProduct("پشمک ساده ۲۰۰ گرمی", "plain-pashmak-200g", "pashmak", 90000m),
                CreateProduct("پشمک شکلاتی ۲۰۰ گرمی", "chocolate-pashmak-200g", "pashmak", 100000m),
                CreateProduct("نی شیر شکلاتی ۲۰ عددی", "chocolate-straw-milk-20pcs", "straw-milk", 50000m),
                CreateProduct("نی شیر توت‌فرنگی ۲۰ عددی", "strawberry-straw-milk-20pcs", "straw-milk", 50000m),
                CreateProduct("بار غلات و شکلات ۴۰ گرمی", "granola-chocolate-bar-40g", "snack-bars", 30000m),
                CreateProduct("بار میوه و آجیل ۴۰ گرمی", "fruit-nut-bar-40g", "snack-bars", 35000m),
                CreateProduct("کیک وانیلی ۴۰ گرمی", "vanilla-cake-40g", "cake", 15000m, brandSlug: "shirin-asal"),
                CreateProduct("کیک شکلاتی ۴۰ گرمی", "chocolate-cake-40g", "cake", 18000m, brandSlug: "shirin-asal"),
                CreateProduct("کلوچه خرمایی ۵۰ گرمی", "date-kolucheh-50g", "kolucheh", 12000m),
                CreateProduct("کلوچه گردویی ۵۰ گرمی", "walnut-kolucheh-50g", "kolucheh", 15000m),
                CreateProduct("کوکی جو دوسر ۱۰۰ گرمی", "oatmeal-cookie-100g", "cookie", 35000m),
                CreateProduct("کوکی شکلات چیپسی ۱۰۰ گرمی", "chocolate-chip-cookie-100g", "cookie", 40000m),
                CreateProduct("کروسان ساده ۶۰ گرمی", "plain-croissant-60g", "croissant", 25000m),
                CreateProduct("کروسان شکلاتی ۶۰ گرمی", "chocolate-croissant-60g", "croissant", 30000m),
                CreateProduct("پیراشکی گوشت ۸۰ گرمی", "meat-pirozhki-80g", "pirozhki-donut", 35000m),
                CreateProduct("دونات شکلاتی ۶۰ گرمی", "chocolate-donut-60g", "pirozhki-donut", 28000m),
                CreateProduct("لواشک آلو ۱۰۰ گرمی", "plum-lavashak-100g", "lavashak", 45000m),
                CreateProduct("لواشک زردآلو ۱۰۰ گرمی", "apricot-lavashak-100g", "lavashak", 50000m),
                CreateProduct("آلوچه جنگلی ۲۰۰ گرمی", "forest-alucheh-200g", "alucheh", 60000m),
                CreateProduct("آلوچه ترش ۲۰۰ گرمی", "sour-alucheh-200g", "alucheh", 55000m),
                CreateProduct("تافی شکلاتی ۵۰ عددی", "chocolate-toffee-50pcs", "toffee", 70000m),
                CreateProduct("تافی میوه‌ای ۵۰ عددی", "fruit-toffee-50pcs", "toffee", 65000m),
                CreateProduct("پنیر سفید ایرانی ۴۰۰ گرمی", "iranian-white-cheese-400g", "white-cheese", 120000m, brandSlug: "mihan"),
                CreateProduct("پنیر سفید کم نمک ۴۰۰ گرمی", "low-salt-white-cheese-400g", "white-cheese", 130000m, brandSlug: "damdaran"),
                CreateProduct("پنیر لاکتیکی ۴۰۰ گرمی", "lactic-cheese-400g", "lactic-cheese", 110000m, brandSlug: "kalleh"),
                CreateProduct("پنیر لاکتیکی کم چرب ۴۰۰ گرمی", "low-fat-lactic-cheese-400g", "lactic-cheese", 100000m, brandSlug: "damdaran")
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Database product seed");

            // ════════════════════════════════════════════════════════
            // بخش ۸: productMap + تصاویر محصولات (۲۶۰ تصویر)
            // ════════════════════════════════════════════════════════
            var productMap = await db.Products
                .Where(p => p.Slug != null)
                .ToDictionaryAsync(p => p.Slug!, p => p.Id);

            var productImages = new List<ProductImage>(productMap.Count * 2);
            foreach (var (slug, pid) in productMap)
            {
                productImages.Add(new ProductImage { ProductId = (int)pid, Path = $"/uploads/products/{slug}-main.jpg", DisplayOrder = 0, IsMain = true, CreatedAt = now, IsDeleted = false });
                productImages.Add(new ProductImage { ProductId = (int)pid, Path = $"/uploads/products/{slug}-alt.jpg", DisplayOrder = 1, IsMain = false, CreatedAt = now, IsDeleted = false });
            }
            db.ProductImages.AddRange(productImages);
            await db.SaveChangesAsync();

            // ════════════════════════════════════════════════════════
            // بخش ۹.۱: ویژگی‌ها (۹ ویژگی — شامل size و material)
            // ════════════════════════════════════════════════════════
            var packagingAttr = new ProductAttribute { Name = "حجم/وزن بسته‌بندی", Slug = "packaging", Type = AttributeType.Select, DisplayOrder = 1, IsFilterable = true, CreatedAt = now };
            var flavorAttr = new ProductAttribute { Name = "طعم", Slug = "flavor", Type = AttributeType.Select, DisplayOrder = 2, IsFilterable = true, CreatedAt = now };
            var sizeAttr = new ProductAttribute { Name = "سایز", Slug = "size", Type = AttributeType.Select, DisplayOrder = 3, IsFilterable = true, CreatedAt = now };
            var materialAttr = new ProductAttribute { Name = "جنس", Slug = "material", Type = AttributeType.Select, DisplayOrder = 4, IsFilterable = false, CreatedAt = now };
            var fatAttr = new ProductAttribute { Name = "درصد چربی", Slug = "fat-content", Type = AttributeType.Select, DisplayOrder = 5, IsFilterable = true, CreatedAt = now };
            var typeAttr = new ProductAttribute { Name = "نوع", Slug = "type", Type = AttributeType.Select, DisplayOrder = 6, IsFilterable = true, CreatedAt = now };
            var colorAttr = new ProductAttribute { Name = "رنگ", Slug = "color", Type = AttributeType.Color, DisplayOrder = 7, IsFilterable = true, CreatedAt = now };
            var countryAttr = new ProductAttribute { Name = "کشور سازنده", Slug = "country-of-origin", Type = AttributeType.Select, DisplayOrder = 8, IsFilterable = true, CreatedAt = now };
            var expiryAttr = new ProductAttribute { Name = "تاریخ انقضا", Slug = "expiry-date", Type = AttributeType.Select, DisplayOrder = 9, IsFilterable = true, CreatedAt = now };

            db.ProductAttributes.AddRange(
                packagingAttr, flavorAttr, sizeAttr, materialAttr,
                fatAttr, typeAttr, colorAttr, countryAttr, expiryAttr);
            await db.SaveChangesAsync();
            logger.LogInformation("Database productAttribute seed");

            // ════════════════════════════════════════════════════════
            // بخش ۹.۲: گزینه‌های ویژگی‌ها (۶۶ گزینه)
            // ════════════════════════════════════════════════════════
            db.ProductAttributeOptions.AddRange(
                // packaging (14)
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۲۵۰ میلی‌لیتر", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۳۳۰ میلی‌لیتر", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۵۰۰ میلی‌لیتر", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۱ لیتر", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۱.۵ لیتر", DisplayOrder = 5, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۲ لیتر", DisplayOrder = 6, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۲.۵ لیتر", DisplayOrder = 7, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۲۵۰ گرم", DisplayOrder = 8, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۵۰۰ گرم", DisplayOrder = 9, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۸۱۰ گرم", DisplayOrder = 10, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۱ کیلوگرم", DisplayOrder = 11, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۱.۸ کیلوگرم", DisplayOrder = 12, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۵ کیلوگرم", DisplayOrder = 13, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = packagingAttr.Id, Value = "۱۰ کیلوگرم", DisplayOrder = 14, CreatedAt = now, IsDeleted = false },
                // flavor (13)
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "وانیلی", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "شکلاتی", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "توت‌فرنگی", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "پرتقالی", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "لیمویی", DisplayOrder = 5, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "سیب", DisplayOrder = 6, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "کلاسیک", DisplayOrder = 7, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "کچاپ", DisplayOrder = 8, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "فلفلی", DisplayOrder = 9, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "نمکی", DisplayOrder = 10, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "پنیری", DisplayOrder = 11, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "دارچینی", DisplayOrder = 12, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = flavorAttr.Id, Value = "نعنایی", DisplayOrder = 13, CreatedAt = now, IsDeleted = false },
                // size (5)
                new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "S", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "M", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "L", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "XL", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = sizeAttr.Id, Value = "XXL", DisplayOrder = 5, CreatedAt = now, IsDeleted = false },
                // material (3)
                new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "چرم", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "پارچه", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = materialAttr.Id, Value = "کتان", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                // fat-content (4)
                new ProductAttributeOption { AttributeId = fatAttr.Id, Value = "بدون چربی", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = fatAttr.Id, Value = "کم چرب", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = fatAttr.Id, Value = "نیم چرب", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = fatAttr.Id, Value = "پر چرب", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                // type (6)
                new ProductAttributeOption { AttributeId = typeAttr.Id, Value = "ساده", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = typeAttr.Id, Value = "خامه‌ای", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = typeAttr.Id, Value = "شکسته", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = typeAttr.Id, Value = "حبه‌ای", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = typeAttr.Id, Value = "پاکتی", DisplayOrder = 5, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = typeAttr.Id, Value = "شیشه‌ای", DisplayOrder = 6, CreatedAt = now, IsDeleted = false },
                // color (5)
                new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "قرمز", ColorCode = "#e74c3c", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "آبی", ColorCode = "#3498db", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "سبز", ColorCode = "#2ecc71", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "مشکی", ColorCode = "#000000", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = colorAttr.Id, Value = "سفید", ColorCode = "#ffffff", DisplayOrder = 5, CreatedAt = now, IsDeleted = false },
                // country-of-origin (10)
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "ایران", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "ترکیه", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "پاکستان", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "هند", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "سریلانکا", DisplayOrder = 5, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "آلمان", DisplayOrder = 6, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "هلند", DisplayOrder = 7, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "سوئیس", DisplayOrder = 8, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "امارات", DisplayOrder = 9, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = countryAttr.Id, Value = "برزیل", DisplayOrder = 10, CreatedAt = now, IsDeleted = false },
                // expiry-date (6)
                new ProductAttributeOption { AttributeId = expiryAttr.Id, Value = "کمتر از ۳ ماه", DisplayOrder = 1, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = expiryAttr.Id, Value = "۳ تا ۶ ماه", DisplayOrder = 2, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = expiryAttr.Id, Value = "۶ تا ۱۲ ماه", DisplayOrder = 3, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = expiryAttr.Id, Value = "۱۲ تا ۱۸ ماه", DisplayOrder = 4, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = expiryAttr.Id, Value = "۱۸ تا ۲۴ ماه", DisplayOrder = 5, CreatedAt = now, IsDeleted = false },
                new ProductAttributeOption { AttributeId = expiryAttr.Id, Value = "بیش از ۲۴ ماه", DisplayOrder = 6, CreatedAt = now, IsDeleted = false }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("Database productAttributeOptions seed");

            // ════════════════════════════════════════════════════════
            // بخش ۱۰: attrMap و optMap
            // ════════════════════════════════════════════════════════
            var attrMap = await db.ProductAttributes
                .ToDictionaryAsync(a => a.Slug, a => a.Id);

            var optMap = await db.ProductAttributeOptions
                .Include(o => o.Attribute)
                .ToDictionaryAsync(o => $"{o.Attribute.Slug}|{o.Value}", o => o.Id);

            // ════════════════════════════════════════════════════════
            // بخش ۱۱: تخصیص ویژگی‌ها به محصولات
            // ════════════════════════════════════════════════════════
            var attrValues = new List<ProductAttributeValue>();

            void Assign(string productSlug, string attrSlug, string optionValue)
            {
                attrValues.Add(new ProductAttributeValue
                {
                    ProductId = (int)productMap[productSlug],
                    AttributeId = attrMap[attrSlug],
                    OptionId = optMap[$"{attrSlug}|{optionValue}"],
                    CreatedAt = now,
                    IsDeleted = false
                });
            }

            // لبنیات — چربی
            Assign("iranian-white-cheese-400g", "fat-content", "نیم چرب");
            Assign("low-salt-white-cheese-400g", "fat-content", "کم چرب");
            Assign("lactic-cheese-400g", "fat-content", "پر چرب");
            Assign("low-fat-lactic-cheese-400g", "fat-content", "کم چرب");

            // نان — نوع
            Assign("bulky-toast-bread-400g", "type", "ساده");
            Assign("burger-buns-6pcs", "type", "ساده");
            Assign("lavash-bread-500g", "type", "ساده");
            Assign("taftoon-bread-400g", "type", "ساده");
            Assign("barbari-bread-500g", "type", "ساده");
            Assign("shirmal-bread-400g", "type", "خامه‌ای");

            // قند و شکر — نوع
            Assign("sugar-cubes-5kg", "type", "حبه‌ای");
            Assign("white-sugar-3kg", "type", "ساده");

            // تنقلات — طعم
            Assign("cheetoz-ketchup-chips-60g", "flavor", "کچاپ");
            Assign("cheetoz-pepper-chips-60g", "flavor", "فلفلی");
            Assign("butter-popcorn-80g", "flavor", "نمکی");
            Assign("salty-popcorn-80g", "flavor", "نمکی");
            Assign("cheesy-crunchy-snack-95g", "flavor", "پنیری");
            Assign("salty-crunchy-snack-95g", "flavor", "نمکی");
            Assign("mint-chewing-gum-20pcs", "flavor", "نعنایی");
            Assign("cinnamon-chewing-gum-20pcs", "flavor", "دارچینی");

            // نوشیدنی — طعم
            Assign("delester-malt-lemon-330ml", "flavor", "لیمویی");
            Assign("delester-malt-classic-330ml", "flavor", "کلاسیک");
            Assign("sparkling-apple-juice-250ml", "flavor", "سیب");
            Assign("sparkling-orange-juice-250ml", "flavor", "پرتقالی");
            Assign("orange-juice-1l", "flavor", "پرتقالی");

            // رنگ
            Assign("vanilla-cake-40g", "color", "سفید");
            Assign("chocolate-cake-40g", "color", "مشکی");

            // کشور سازنده
            Assign("basmati-foreign-rice-5kg", "country-of-origin", "هند");
            Assign("pakistani-rice-5kg", "country-of-origin", "پاکستان");
            Assign("ceylon-black-tea-400g", "country-of-origin", "سریلانکا");
            Assign("assam-black-tea-400g", "country-of-origin", "هند");
            Assign("redbull-energy-drink-250ml", "country-of-origin", "سوئیس");
            Assign("hashemi-iranian-rice-5kg", "country-of-origin", "ایران");
            Assign("iranian-black-tea-500g", "country-of-origin", "ایران");
            Assign("iranian-white-cheese-400g", "country-of-origin", "ایران");
            Assign("coca-cola-1.5l", "country-of-origin", "ایران");
            Assign("cheetoz-ketchup-chips-60g", "country-of-origin", "ایران");
            Assign("atiyeh-bulgur-wheat-550g", "country-of-origin", "ایران");
            Assign("zar-macaron-spaghetti-500g", "country-of-origin", "ایران");
            Assign("delester-malt-lemon-330ml", "country-of-origin", "ایران");

            // تاریخ انقضا
            Assign("bulky-toast-bread-400g", "expiry-date", "کمتر از ۳ ماه");
            Assign("burger-buns-6pcs", "expiry-date", "کمتر از ۳ ماه");
            Assign("lavash-bread-500g", "expiry-date", "کمتر از ۳ ماه");
            Assign("taftoon-bread-400g", "expiry-date", "کمتر از ۳ ماه");
            Assign("barbari-bread-500g", "expiry-date", "کمتر از ۳ ماه");
            Assign("shirmal-bread-400g", "expiry-date", "کمتر از ۳ ماه");
            Assign("iranian-white-cheese-400g", "expiry-date", "۳ تا ۶ ماه");
            Assign("low-salt-white-cheese-400g", "expiry-date", "۳ تا ۶ ماه");
            Assign("lactic-cheese-400g", "expiry-date", "۳ تا ۶ ماه");
            Assign("low-fat-lactic-cheese-400g", "expiry-date", "۳ تا ۶ ماه");
            Assign("khomali-dough-1l", "expiry-date", "۳ تا ۶ ماه");
            Assign("carbonated-dough-300ml", "expiry-date", "۳ تا ۶ ماه");
            Assign("coca-cola-1.5l", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("sprite-1.5l", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("redbull-energy-drink-250ml", "expiry-date", "۱۲ تا ۱۸ ماه");
            Assign("hype-energy-drink-250ml", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("delester-malt-lemon-330ml", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("mixed-juice-large-1l", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("orange-juice-1l", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("mineral-water-large-1.5l", "expiry-date", "بیش از ۲۴ ماه");
            Assign("cheetoz-ketchup-chips-60g", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("cheetoz-pepper-chips-60g", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("butter-popcorn-80g", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("mint-chewing-gum-20pcs", "expiry-date", "۱۸ تا ۲۴ ماه");
            Assign("plain-wheat-biscuit-200g", "expiry-date", "۶ تا ۱۲ ماه");
            Assign("dark-chocolate-bar-100g", "expiry-date", "۱۲ تا ۱۸ ماه");
            Assign("hashemi-iranian-rice-5kg", "expiry-date", "بیش از ۲۴ ماه");
            Assign("pakistani-rice-5kg", "expiry-date", "بیش از ۲۴ ماه");
            Assign("atiyeh-bulgur-wheat-550g", "expiry-date", "۱۲ تا ۱۸ ماه");
            Assign("atiyeh-chickpeas-500g", "expiry-date", "۱۲ تا ۱۸ ماه");
            Assign("zar-macaron-spaghetti-500g", "expiry-date", "۱۸ تا ۲۴ ماه");
            Assign("sugar-cubes-5kg", "expiry-date", "بیش از ۲۴ ماه");
            Assign("iranian-black-tea-500g", "expiry-date", "بیش از ۲۴ ماه");
            Assign("ceylon-black-tea-400g", "expiry-date", "بیش از ۲۴ ماه");
            Assign("liquid-frying-oil-810g", "expiry-date", "۱۲ تا ۱۸ ماه");
            Assign("extra-virgin-olive-oil-500ml", "expiry-date", "۱۸ تا ۲۴ ماه");

            db.ProductAttributeValues.AddRange(attrValues);
            await db.SaveChangesAsync();
            logger.LogInformation("Database productAttributeValue seed");

            // ════════════════════════════════════════════════════════
            // بخش ۱۲: تنوع‌های محصول (ProductVariants — ۱۷ تنوع)
            // ════════════════════════════════════════════════════════
            var variants = new List<ProductVariant>();

            void AddVariant(string productSlug, string skuSuffix, decimal price,
                            decimal? discountPrice, int stock, string packagingValue)
            {
                variants.Add(new ProductVariant
                {
                    ProductId = (int)productMap[productSlug],
                    Sku = $"{productSlug}-{skuSuffix}",
                    Price = price,
                    DiscountPrice = discountPrice,
                    Stock = stock,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = null,
                    IsDeleted = false,
                    AttributeValues = new List<ProductVariantAttributeValue>
                {
                    new()
                    {
                        AttributeId = attrMap["packaging"],
                        OptionId    = optMap[$"packaging|{packagingValue}"],
                        CreatedAt   = now,
                        IsDeleted   = false
                    }
                }
                });
            }

            AddVariant("mineral-water-small-500ml", "1-5l", 25000m, null, 100, "۱.۵ لیتر");
            AddVariant("mineral-water-small-500ml", "2l", 35000m, null, 80, "۲ لیتر");
            AddVariant("coca-cola-1.5l", "330ml", 35000m, null, 200, "۳۳۰ میلی‌لیتر");
            AddVariant("coca-cola-1.5l", "2-5l", 140000m, null, 60, "۲.۵ لیتر");
            AddVariant("sprite-1.5l", "330ml", 35000m, null, 180, "۳۳۰ میلی‌لیتر");
            AddVariant("sprite-1.5l", "2-5l", 135000m, null, 50, "۲.۵ لیتر");
            AddVariant("sour-cherry-syrup-1l", "500ml", 100000m, null, 60, "۵۰۰ میلی‌لیتر");
            AddVariant("orange-syrup-1l", "500ml", 90000m, null, 60, "۵۰۰ میلی‌لیتر");
            AddVariant("khomali-dough-1l", "500ml", 80000m, null, 50, "۵۰۰ میلی‌لیتر");
            AddVariant("khomali-dough-1l", "1-5l", 220000m, null, 30, "۱.۵ لیتر");
            AddVariant("liquid-frying-oil-810g", "1-8kg", 260000m, null, 40, "۱.۸ کیلوگرم");
            AddVariant("extra-virgin-olive-oil-500ml", "250ml", 180000m, null, 30, "۲۵۰ میلی‌لیتر");
            AddVariant("extra-virgin-olive-oil-500ml", "1l", 680000m, null, 20, "۱ لیتر");
            AddVariant("hashemi-iranian-rice-5kg", "10kg", 2350000m, null, 15, "۱۰ کیلوگرم");
            AddVariant("mixed-juice-large-1l", "250ml", 25000m, null, 100, "۲۵۰ میلی‌لیتر");
            AddVariant("orange-juice-1l", "250ml", 27000m, null, 100, "۲۵۰ میلی‌لیتر");
            AddVariant("redbull-energy-drink-250ml", "330ml", 120000m, null, 80, "۳۳۰ میلی‌لیتر");

            db.ProductVariants.AddRange(variants);
            await db.SaveChangesAsync();
            logger.LogInformation("Database productVariant seed");

            // ════════════════════════════════════════════════════════
            // بخش ۱۳: به‌روزرسانی HasVariants
            // ════════════════════════════════════════════════════════
            var variantProductSlugs = new[]
            {
            "mineral-water-small-500ml", "coca-cola-1.5l", "sprite-1.5l",
            "sour-cherry-syrup-1l", "orange-syrup-1l", "khomali-dough-1l",
            "liquid-frying-oil-810g", "extra-virgin-olive-oil-500ml",
            "hashemi-iranian-rice-5kg", "mixed-juice-large-1l",
            "orange-juice-1l", "redbull-energy-drink-250ml"
        };

            await db.Products
                .Where(p => variantProductSlugs.Contains(p.Slug!))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.HasVariants, true)
                    .SetProperty(p => p.UpdatedAt, now));

            logger.LogInformation("Database variantProductSlugs seed");

            logger.LogInformation("Database initialization completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed");
            throw;
        }
    }
}