using Stripe;
using Stripe.Checkout;

namespace BioscoopMAUI.API.Services;

public class StripeService(IConfiguration configuration) : IStripeService
{
    private static readonly StripeWebhookSessionResult InvalidWebhook = new(false, false, null, null);
    private const string CheckoutSessionCompleted = "checkout.session.completed";
    private const string HoldIdKey = "hold_id";
    
    public async Task<StripeSessionResult> CreateSessionAsync(Guid holdId, decimal totalPrice, string description, string successUrl, string cancelUrl)
    {
        var amountInCents = decimal.ToInt64(Math.Round(totalPrice * 100m, 0, MidpointRounding.AwayFromZero));
        var currency = configuration["Stripe:Currency"] ?? "eur";
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = amountInCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = description
                        }
                    }
                }
            ],
            Metadata = new Dictionary<string, string>
            {
                [HoldIdKey] = holdId.ToString()
            }
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(options, CreateRequestOptions());
        var sessionId = session.Id ?? string.Empty;
        var checkoutUrl = session.Url ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(checkoutUrl))
            throw new InvalidOperationException("Stripe did not return a valid Checkout Session.");

        return new StripeSessionResult(sessionId, checkoutUrl);
    }

    public async Task<StripeSessionStatusResult> GetSessionStatusAsync(string sessionId)
    {
        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(sessionId, requestOptions: CreateRequestOptions());
        return ParseSessionStatus(session);
    }

    public async Task RefundPaymentAsync(string paymentIntentId)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId
        };

        var refundService = new RefundService();
        await refundService.CreateAsync(options, CreateRequestOptions());
    }

    public StripeWebhookSessionResult GetWebhookSessionResult(string payload, string stripeSignature)
    {
        var webhookSecret = configuration["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(webhookSecret))
            return InvalidWebhook;

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, stripeSignature, webhookSecret);
            if (!string.Equals(stripeEvent.Type, CheckoutSessionCompleted, StringComparison.OrdinalIgnoreCase))
                return new StripeWebhookSessionResult(true, false, null, null);

            if (stripeEvent.Data.Object is not Session session)
                return InvalidWebhook;

            return new StripeWebhookSessionResult(true, true, session.Id, ParseSessionStatus(session));
        }
        catch (StripeException)
        {
            return InvalidWebhook;
        }
    }

    private RequestOptions CreateRequestOptions()
    {
        var restrictedKey = configuration["Stripe:RestrictedKey"];
        if (string.IsNullOrWhiteSpace(restrictedKey))
            throw new InvalidOperationException("Stripe:RestrictedKey is not configured.");

        return new RequestOptions
        {
            ApiKey = restrictedKey
        };
    }

    private static StripeSessionStatusResult ParseSessionStatus(Session session)
    {
        Guid? holdId = null;
        if (session.Metadata.TryGetValue(HoldIdKey, out var holdIdValue)
            && Guid.TryParse(holdIdValue, out var parsedHoldId))
        {
            holdId = parsedHoldId;
        }

        return new StripeSessionStatusResult(session.PaymentStatus ?? string.Empty, session.PaymentIntentId, holdId);
    }
}