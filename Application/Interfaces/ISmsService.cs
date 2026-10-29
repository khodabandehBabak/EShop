namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس ارسال پیامک (Kavenegar / Twilio / ...)
/// </summary>
public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message);
}