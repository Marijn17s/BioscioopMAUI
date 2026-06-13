namespace BioscoopMAUI.API.Services;

public interface IStripeService
{
    Task<StripeSessionResult> CreateSessionAsync(Guid holdId, decimal totalPrice, string description, string successUrl, string cancelUrl);
    Task<StripeSessionStatusResult> GetSessionStatusAsync(string sessionId);
    Task RefundPaymentAsync(string paymentIntentId);
    StripeWebhookSessionResult GetWebhookSessionResult(string payload, string stripeSignature);
}