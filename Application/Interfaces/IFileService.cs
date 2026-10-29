using Microsoft.AspNetCore.Http;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس ذخیره‌سازی فایل (اینجا: Local)
/// </summary>
public interface IFileService
{
    /// <summary>ذخیره‌ی فایل تصویری و بازگشت URL نسبی</summary>
    Task<string> SaveImageAsync(IFormFile file, string subFolder, int? maxWidth = null);

    /// <summary>حذف فایل با URL نسبی</summary>
    Task DeleteAsync(string? relativeUrl);

    /// <summary>اعتبارسنجی فایل تصویری (پسوند و حجم)</summary>
    (bool IsValid, string? Error) ValidateImage(IFormFile file, long maxSizeInMb = 2);
}