namespace EShop.Application.DTOs;

public record PaymentRequestDto(
    string OrderNumber,
    decimal Amount,
    string Description,
    string CallbackUrl);

public record PaymentRequestResult(
    bool Success,
    string Authority,
    string RedirectUrl,
    string? ErrorMessage = null);

public record PaymentVerifyResult(
    bool Success,
    string? RefId,
    string? ErrorMessage = null);