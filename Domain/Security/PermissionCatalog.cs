namespace EShop.Domain.Security;

public static class PermissionCatalog
{
    public const string DashboardView = "dashboard.view";

    public const string ProductsView = "products.view";
    public const string ProductsCreate = "products.create";
    public const string ProductsEdit = "products.edit";
    public const string ProductsDelete = "products.delete";

    public const string CategoriesView = "categories.view";
    public const string CategoriesCreate = "categories.create";
    public const string CategoriesEdit = "categories.edit";
    public const string CategoriesDelete = "categories.delete";

    public const string BrandsView = "brands.view";
    public const string BrandsCreate = "brands.create";
    public const string BrandsEdit = "brands.edit";
    public const string BrandsDelete = "brands.delete";

    public const string AttributesView = "attributes.view";
    public const string AttributesCreate = "attributes.create";
    public const string AttributesEdit = "attributes.edit";
    public const string AttributesDelete = "attributes.delete";

    public const string OrdersView = "orders.view";
    public const string OrdersProcess = "orders.process";
    public const string OrdersPack = "orders.pack";
    public const string OrdersShip = "orders.ship";
    public const string OrdersDeliver = "orders.deliver";
    public const string OrdersCancel = "orders.cancel";
    public const string OrdersDelete = "orders.delete";

    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";

    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesEdit = "roles.edit";
    public const string RolesDelete = "roles.delete";

    public const string PermissionsView = "permissions.view";

    public const string SlidersView = "sliders.view";
    public const string SlidersCreate = "sliders.create";
    public const string SlidersEdit = "sliders.edit";
    public const string SlidersDelete = "sliders.delete";

    public const string SettingsView = "settings.view";
    public const string SettingsEdit = "settings.edit";

    public const string ReportsView = "reports.view";

    public const string ReviewsView = "reviews.view";
    public const string ReviewsModerate = "reviews.moderate";
    public const string ReviewsDelete = "reviews.delete";

    public static List<(string Code, string Name, string Group, int Order)> All => new()
    {
        (DashboardView, "مشاهده داشبورد", "داشبورد", 1),

        (ProductsView, "مشاهده محصولات", "محصولات", 10),
        (ProductsCreate, "ایجاد محصول", "محصولات", 11),
        (ProductsEdit, "ویرایش محصول", "محصولات", 12),
        (ProductsDelete, "حذف محصول", "محصولات", 13),

        (CategoriesView, "مشاهده دسته‌بندی", "دسته‌بندی", 20),
        (CategoriesCreate, "ایجاد دسته‌بندی", "دسته‌بندی", 21),
        (CategoriesEdit, "ویرایش دسته‌بندی", "دسته‌بندی", 22),
        (CategoriesDelete, "حذف دسته‌بندی", "دسته‌بندی", 23),

        (BrandsView, "مشاهده برندها", "برندها", 30),
        (BrandsCreate, "ایجاد برند", "برندها", 31),
        (BrandsEdit, "ویرایش برند", "برندها", 32),
        (BrandsDelete, "حذف برند", "برندها", 33),

        (AttributesView, "مشاهده ویژگی‌ها", "ویژگی‌ها", 35),
        (AttributesCreate, "ایجاد ویژگی", "ویژگی‌ها", 36),
        (AttributesEdit, "ویرایش ویژگی", "ویژگی‌ها", 37),
        (AttributesDelete, "حذف ویژگی", "ویژگی‌ها", 38),

        (OrdersView, "مشاهده سفارشات", "سفارشات", 40),
        (OrdersProcess, "پردازش سفارش", "سفارشات", 41),
        (OrdersPack, "بسته‌بندی سفارش", "سفارشات", 42),
        (OrdersShip, "ارسال سفارش", "سفارشات", 43),
        (OrdersDeliver, "تأیید تحویل", "سفارشات", 44),
        (OrdersCancel, "لغو سفارش", "سفارشات", 45),
        (OrdersDelete, "حذف سفارش", "سفارشات", 46),

        (UsersView, "مشاهده کاربران", "کاربران", 50),
        (UsersCreate, "ایجاد کاربر", "کاربران", 51),
        (UsersEdit, "ویرایش کاربر", "کاربران", 52),
        (UsersDelete, "حذف کاربر", "کاربران", 53),

        (RolesView, "مشاهده نقش‌ها", "نقش‌ها", 60),
        (RolesCreate, "ایجاد نقش", "نقش‌ها", 61),
        (RolesEdit, "ویرایش نقش", "نقش‌ها", 62),
        (RolesDelete, "حذف نقش", "نقش‌ها", 63),

        (PermissionsView, "مشاهده مجوزها", "مجوزها", 70),

        (SlidersView, "مشاهده اسلایدرها", "اسلایدرها", 80),
        (SlidersCreate, "ایجاد اسلایدر", "اسلایدرها", 81),
        (SlidersEdit, "ویرایش اسلایدر", "اسلایدرها", 82),
        (SlidersDelete, "حذف اسلایدر", "اسلایدرها", 83),

        (SettingsView, "مشاهده تنظیمات", "تنظیمات", 90),
        (SettingsEdit, "ویرایش تنظیمات", "تنظیمات", 91),

        (ReportsView, "مشاهده گزارشات", "گزارشات", 100),

        (ReviewsView, "مشاهده نظرات", "نظرات", 110),
        (ReviewsModerate, "تأیید/رد نظرات", "نظرات", 111),
        (ReviewsDelete, "حذف نظرات", "نظرات", 112),
    };
}