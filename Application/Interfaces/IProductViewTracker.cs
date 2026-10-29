namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس ردیابی بازدید محصول (Session dedup + Batch flush)
/// </summary>
public interface IProductViewTracker
{
    /// <summary>ثبت یک بازدید با بررسی تکراری</summary>
    Task TrackAsync(int productId);

    /// <summary>ذخیره‌ی pending updates در دیتابیس</summary>
    Task FlushAsync();

    /// <summary>تعداد pending (برای debug و background flush)</summary>
    int PendingCount { get; }
}