namespace BioscoopMAUI.API.Services;

public record StripeSessionResult(
    string SessionId,
    string CheckoutUrl);

public record StripeSessionStatusResult(
    string Status,
    string? PaymentIntentId,
    Guid? HoldId);

public record StripeWebhookSessionResult(
    bool IsValid,
    bool IsCheckoutSessionCompleted,
    string? SessionId,
    StripeSessionStatusResult? SessionStatus);