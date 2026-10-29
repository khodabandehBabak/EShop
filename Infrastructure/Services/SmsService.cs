using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using EShop.Application.Interfaces;

namespace EShop.Infrastructure.Services;

/// <summary>
/// سرویس پیامک — در حالت توسعه فقط لاگ می‌کند
/// برای استفاده واقعی، اطلاعات Kavenegar را در appsettings تنظیم کنید
/// </summary>
public class SmsService : ISmsService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmsService> _logger;

    public SmsService(IConfiguration config, ILogger<SmsService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        var apiKey = _config["Sms:KavenegarApiKey"];
        var isDevelopment = _config["ASPNETCORE_ENVIRONMENT"] == "Development"
            || string.IsNullOrWhiteSpace(apiKey);

        if (isDevelopment)
        {
            _logger.LogInformation(
                "[SMS-DEV] To: {Phone}, Message: {Message}",
                phoneNumber, message);
            await Task.CompletedTask;
            return;
        }

        // ============ Production: Kavenegar API ============
        try
        {
            using var http = new HttpClient();
            var url = $"https://api.kavenegar.com/v1/{apiKey}/sms/send.json" +
                      $"?receptor={Uri.EscapeDataString(phoneNumber)}" +
                      $"&message={Uri.EscapeDataString(message)}" +
                      $"&sender={_config["Sms:SenderNumber"]}";

            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("SMS send failed: {Status}",
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send exception to {Phone}", phoneNumber);
        }
    }
}