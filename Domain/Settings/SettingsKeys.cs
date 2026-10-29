namespace EShop.Domain.Settings;

public static class SettingsKeys
{
    // General
    public const string SiteName = "site.name";
    public const string SiteTagline = "site.tagline";
    public const string SiteDescription = "site.description";
    public const string SiteKeywords = "site.keywords";
    public const string DefaultCurrency = "site.currency";
    public const string TimeZone = "site.timezone";

    // Branding
    public const string LogoUrl = "branding.logo";
    public const string FooterLogoUrl = "branding.footer_logo";
    public const string FaviconUrl = "branding.favicon";
    public const string LogoWidth = "branding.logo_width";
    public const string LogoHeight = "branding.logo_height";

    // Contact
    public const string ContactPhone = "contact.phone";
    public const string ContactMobile = "contact.mobile";
    public const string ContactEmail = "contact.email";
    public const string ContactAddress = "contact.address";
    public const string ContactPostalCode = "contact.postal_code";
    public const string ContactMapUrl = "contact.map_url";
    public const string ContactWorkHours = "contact.work_hours";

    // Social
    public const string SocialFacebook = "social.facebook";
    public const string SocialInstagram = "social.instagram";
    public const string SocialTelegram = "social.telegram";
    public const string SocialTwitter = "social.twitter";
    public const string SocialYoutube = "social.youtube";
    public const string SocialLinkedin = "social.linkedin";
    public const string SocialWhatsapp = "social.whatsapp";
    public const string SocialAparat = "social.aparat";

    // Shipping
    public const string ShippingFreeThreshold = "shipping.free_threshold";
    public const string ShippingNormalCost = "shipping.normal_cost";
    public const string ShippingExpressCost = "shipping.express_cost";
    public const string ShippingPickupCost = "shipping.pickup_cost";
    public const string ShippingExpressCities = "shipping.express_cities";
    public const string ShippingNormalDays = "shipping.normal_days";

    // Payment
    public const string PaymentZarinpalMerchantId = "payment.zarinpal.merchant_id";
    public const string PaymentZarinpalSandbox = "payment.zarinpal.sandbox";
    public const string PaymentCodEnabled = "payment.cod.enabled";
    public const string PaymentOnlineEnabled = "payment.online.enabled";
    public const string PaymentCodFee = "payment.cod.fee";

    // SEO
    public const string SeoDefaultTitle = "seo.default_title";
    public const string SeoDefaultDescription = "seo.default_description";
    public const string SeoDefaultKeywords = "seo.default_keywords";
    public const string SeoGoogleAnalytics = "seo.google_analytics";
    public const string SeoGoogleSearchConsole = "seo.google_search_console";
    public const string SeoRobotsEnabled = "seo.robots_enabled";

    // System
    public const string MaintenanceMode = "system.maintenance_mode";
    public const string MaintenanceMessage = "system.maintenance_message";
}