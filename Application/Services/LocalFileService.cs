using Microsoft.AspNetCore.Hosting;   // ← برای IWebHostEnvironment
using Microsoft.AspNetCore.Http;       // ← برای IFormFile
using EShop.Application.Interfaces;

namespace EShop.Application.Services;

public class LocalFileService : IFileService
{
    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".svg" };

    private readonly IWebHostEnvironment _env;

    public LocalFileService(IWebHostEnvironment env) => _env = env;

    public (bool IsValid, string? Error) ValidateImage(IFormFile file, long maxSizeInMb = 2)
    {
        if (file == null || file.Length == 0)
            return (false, "فایل انتخاب نشده است");

        if (file.Length > maxSizeInMb * 1024 * 1024)
            return (false, $"حجم فایل نباید بیشتر از {maxSizeInMb} مگابایت باشد");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (false, "فرمت مجاز: JPG, PNG, WEBP, SVG");

        return (true, null);
    }

    public async Task<string> SaveImageAsync(IFormFile file, string subFolder, int? maxWidth = null)
    {
        var (isValid, error) = ValidateImage(file);
        if (!isValid) throw new InvalidOperationException(error);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";

        var folderPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", subFolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/{subFolder}/{fileName}";
    }

    public Task DeleteAsync(string? relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl)) return Task.CompletedTask;
        if (!relativeUrl.StartsWith("/uploads/")) return Task.CompletedTask;

        var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot",
            relativeUrl.TrimStart('/'));
        if (File.Exists(fullPath))
        {
            try { File.Delete(fullPath); } catch { /* ignore */ }
        }
        return Task.CompletedTask;
    }
}