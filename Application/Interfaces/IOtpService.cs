namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس OTP برای ورود مشتری
/// </summary>
public interface IOtpService
{
    /// <summary>تولید کد OTP و ارسال پیامک</summary>
    Task<string> GenerateAndSendAsync(string phoneNumber);

    /// <summary>بررسی صحت کد</summary>
    Task<bool> VerifyAsync(string phoneNumber, string code);
}