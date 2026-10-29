using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Settings;

namespace EShop.Infrastructure.Services;

/// <summary>
/// درگاه پرداخت زرین‌پال
/// Merchant ID و Sandbox از SiteSettings خوانده می‌شود (قابل تنظیم در پنل ادمین)
/// </summary>
public class ZarinpalGateway : IPaymentGateway
{
    private const string LiveRequestUrl = "https://api.zarinpal.com/pg/v4/payment/request.json";
    private const string LiveVerifyUrl = "https://api.zarinpal.com/pg/v4/payment/verify.json";
    private const string LiveStartPay = "https://www.zarinpal.com/pg/StartPay/";

    private const string SandboxRequestUrl = "https://sandbox.zarinpal.com/pg/v4/payment/request.json";
    private const string SandboxVerifyUrl = "https://sandbox.zarinpal.com/pg/v4/payment/verify.json";
    private const string SandboxStartPay = "https://sandbox.zarinpal.com/pg/StartPay/";

    private readonly HttpClient _http;
    private readonly ISettingsService _settings;
    private readonly ILogger<ZarinpalGateway> _logger;

    public ZarinpalGateway(
        HttpClient http,
        ISettingsService settings,
        ILogger<ZarinpalGateway> logger)
    {
        _http = http;
        _settings = settings;
        _logger = logger;
    }

    private async Task<(string MerchantId, bool IsSandbox)> GetConfigAsync()
    {
        var merchantId = await _settings.GetAsync(
            SettingsKeys.PaymentZarinpalMerchantId, "") ?? "";
        var isSandbox = await _settings.GetBoolAsync(
            SettingsKeys.PaymentZarinpalSandbox, true);
        return (merchantId, isSandbox);
    }

    // ============================================================
    // REQUEST
    // ============================================================
    public async Task<PaymentRequestResult> RequestAsync(PaymentRequestDto req)
    {
        try
        {
            var (merchantId, isSandbox) = await GetConfigAsync();
            if (string.IsNullOrWhiteSpace(merchantId))
            {
                _logger.LogWarning("ZarinPal MerchantId is not configured");
                return new PaymentRequestResult(false, "", "",
                    "تنظیمات درگاه پرداخت کامل نیست. با مدیر تماس بگیرید.");
            }

            var requestUrl = isSandbox ? SandboxRequestUrl : LiveRequestUrl;
            var startPayBase = isSandbox ? SandboxStartPay : LiveStartPay;

            // zarinpal v4 amount in Rial
            var amountInRial = (long)(req.Amount * 10);

            var payload = new
            {
                merchant_id = merchantId,
                amount = amountInRial,
                description = req.Description,
                callback_url = req.CallbackUrl,
                metadata = new { order_number = req.OrderNumber }
            };

            _logger.LogInformation(
                "ZarinPal request: {Order} - {Amount} - Sandbox: {Sandbox}",
                req.OrderNumber, req.Amount, isSandbox);

            var response = await _http.PostAsJsonAsync(requestUrl, payload);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (json.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
            {
                var code = data.GetProperty("code").GetInt32();
                var authority = data.GetProperty("authority").GetString();
                if (code == 100 && !string.IsNullOrEmpty(authority))
                {
                    return new PaymentRequestResult(
                        true,
                        authority,
                        startPayBase + authority);
                }
            }

            var errorMsg = "خطای نامشخص از درگاه";
            if (json.TryGetProperty("errors", out var errors) && errors.ValueKind != JsonValueKind.Null)
            {
                if (errors.TryGetProperty("message", out var msg))
                    errorMsg = msg.GetString() ?? errorMsg;
            }

            _logger.LogWarning("ZarinPal request failed: {Error}", errorMsg);
            return new PaymentRequestResult(false, "", "", errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal request exception");
            return new PaymentRequestResult(false, "", "",
                "خطا در ارتباط با درگاه پرداخت. لطفاً دوباره تلاش کنید.");
        }
    }

    // ============================================================
    // VERIFY
    // ============================================================
    public async Task<PaymentVerifyResult> VerifyAsync(string authority, decimal amount)
    {
        try
        {
            var (merchantId, isSandbox) = await GetConfigAsync();
            var verifyUrl = isSandbox ? SandboxVerifyUrl : LiveVerifyUrl;

            var amountInRial = (long)(amount * 10);

            var payload = new
            {
                merchant_id = merchantId,
                amount = amountInRial,
                authority
            };

            _logger.LogInformation("ZarinPal verify: Authority={Authority}", authority);

            var response = await _http.PostAsJsonAsync(verifyUrl, payload);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (json.TryGetProperty("data", out var data) && data.ValueKind != JsonValueKind.Null)
            {
                var code = data.GetProperty("code").GetInt32();
                // 100 = موفق، 101 = قبلاً تأیید شده
                if (code == 100 || code == 101)
                {
                    var refId = data.GetProperty("ref_id").GetInt64().ToString();
                    _logger.LogInformation("ZarinPal verify success: RefId={RefId}", refId);
                    return new PaymentVerifyResult(true, refId);
                }
            }

            var errorMsg = "پرداخت تأیید نشد";
            if (json.TryGetProperty("errors", out var errors) && errors.ValueKind != JsonValueKind.Null)
            {
                if (errors.TryGetProperty("message", out var msg))
                    errorMsg = msg.GetString() ?? errorMsg;
            }

            _logger.LogWarning("ZarinPal verify failed: {Error}", errorMsg);
            return new PaymentVerifyResult(false, null, errorMsg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal verify exception");
            return new PaymentVerifyResult(false, null, "خطا در تأیید پرداخت");
        }
    }
}