using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// درگاه پرداخت (Zarinpal پیاده‌سازی می‌کند)
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentRequestResult> RequestAsync(PaymentRequestDto req);
    Task<PaymentVerifyResult> VerifyAsync(string authority, decimal amount);
}