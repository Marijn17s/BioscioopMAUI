using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.API.Extensions;
using BioscoopMAUI.API.Services;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController(
    BioscoopDbContext context,
    ITicketPricingService ticketPricingService,
    IStripeService stripeService,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("checkout-session")]
    [Authorize]
    public async Task<ActionResult<CheckoutSessionResponseDto>> CreateCheckoutSession([FromBody] CheckoutSessionRequestDto request)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var heldSeats = await context.ShowtimeSeats
            .Where(ss => ss.HoldId == request.HoldId
                && ss.HoldAuth0UserId == auth0UserId
                && ss.HoldExpiresAtUtc > DateTime.UtcNow
                && !ss.ReservationId.HasValue)
            .ToListAsync();

        if (heldSeats.Count is 0)
            return BadRequest("Seat hold was not found or has expired");

        var showtimeId = heldSeats[0].ShowtimeId;
        if (heldSeats.Any(seat => seat.ShowtimeId != showtimeId))
            return BadRequest("Seat hold contains invalid seats");

        var showtime = await context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        var priceQuote = await ticketPricingService.GetPriceQuoteAsync(showtime, heldSeats.Count);
        var description = $"{showtime.Movie.Title} tickets";
        var successUrl = $"{Request.Scheme}://{Request.Host}/api/payment/return?session_id={{CHECKOUT_SESSION_ID}}&result=success";
        var cancelUrl = $"{Request.Scheme}://{Request.Host}/api/payment/return?session_id={{CHECKOUT_SESSION_ID}}&result=cancel";
        var session = await stripeService.CreateSessionAsync(request.HoldId, priceQuote.TotalPrice, description, successUrl, cancelUrl);

        return Ok(new CheckoutSessionResponseDto(
            session.SessionId,
            session.CheckoutUrl,
            heldSeats.Min(seat => seat.HoldExpiresAtUtc) ?? DateTime.UtcNow));
    }

    [HttpGet("status/{sessionId}")]
    [Authorize]
    public async Task<ActionResult<PaymentStatusResponseDto>> GetPaymentStatus(string sessionId)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var existingReservation = await context.Reservations.FirstOrDefaultAsync(r => r.StripeSessionId == sessionId);
        if (existingReservation is not null)
        {
            if (existingReservation.Auth0UserId != auth0UserId)
                return NotFound();

            return Ok(new PaymentStatusResponseDto("paid", existingReservation.Id, null));
        }

        var sessionStatus = await stripeService.GetSessionStatusAsync(sessionId);
        if (!string.Equals(sessionStatus.Status, "paid", StringComparison.OrdinalIgnoreCase))
            return Ok(new PaymentStatusResponseDto(sessionStatus.Status, null, null));

        if (sessionStatus.HoldId is null)
            return Ok(new PaymentStatusResponseDto("paid", null, "Payment succeeded, but the seat hold could not be found."));

        var reservationId = await FinalizePaidHoldAsync(sessionStatus.HoldId.Value, sessionId, sessionStatus.PaymentIntentId);
        return Ok(new PaymentStatusResponseDto("paid", reservationId, null));
    }

    [HttpGet("return")]
    public IActionResult ReturnFromCheckout([FromQuery(Name = "session_id")] string sessionId, [FromQuery] string result)
    {
        var appReturnUrl = configuration["Stripe:AppReturnUrl"];
        if (string.IsNullOrWhiteSpace(appReturnUrl))
            return BadRequest();

        return Redirect($"{appReturnUrl}?session_id={Uri.EscapeDataString(sessionId)}&result={Uri.EscapeDataString(result)}");
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(stripeSignature))
            return BadRequest();

        var webhookSession = stripeService.GetWebhookSessionResult(payload, stripeSignature);
        if (!webhookSession.IsValid)
            return BadRequest();

        if (!webhookSession.IsCheckoutSessionCompleted)
            return Ok();

        var sessionStatus = webhookSession.SessionStatus;
        var sessionId = webhookSession.SessionId;

        if (sessionStatus?.HoldId is not null && !string.IsNullOrWhiteSpace(sessionId))
            await FinalizePaidHoldAsync(sessionStatus.HoldId.Value, sessionId, sessionStatus.PaymentIntentId);

        return Ok();
    }

    [HttpPost("pin")]
    public async Task<ActionResult> ValidatePin([FromBody] string pinCode)
    {
        var isValid = await context.PinCards.AnyAsync(p => p.PinCode == pinCode);
        if (!isValid)
            return BadRequest(new { message = "Ongeldige pincode" });
        return Ok(new {message = "Betaling voltooid" });
    }

    private async Task<int> FinalizePaidHoldAsync(Guid holdId, string sessionId, string? paymentIntentId)
    {
        var existingReservation = await context.Reservations.FirstOrDefaultAsync(r => r.StripeSessionId == sessionId);
        if (existingReservation is not null)
            return existingReservation.Id;

        var heldSeats = await context.ShowtimeSeats
            .Where(ss => ss.HoldId == holdId && !ss.ReservationId.HasValue)
            .ToListAsync();

        if (heldSeats.Count is 0)
            throw new InvalidOperationException("Seat hold was not found or has expired.");

        var showtimeId = heldSeats[0].ShowtimeId;
        var showtime = await context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            throw new InvalidOperationException("Showtime was not found.");

        var auth0UserId = heldSeats[0].HoldAuth0UserId;
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new InvalidOperationException("Seat hold is not associated with a user.");

        var priceQuote = await ticketPricingService.GetPriceQuoteAsync(showtime, heldSeats.Count);
        var reservation = new Reservation
        {
            ShowtimeId = showtimeId,
            Auth0UserId = auth0UserId,
            TotalPrice = priceQuote.TotalPrice,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTime.UtcNow,
            StripeSessionId = sessionId,
            StripePaymentIntentId = paymentIntentId
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        foreach (var showtimeSeat in heldSeats)
        {
            showtimeSeat.ReservationId = reservation.Id;
            showtimeSeat.HoldId = null;
            showtimeSeat.HoldAuth0UserId = null;
            showtimeSeat.HoldExpiresAtUtc = null;
        }

        await context.SaveChangesAsync();
        return reservation.Id;
    }
}
