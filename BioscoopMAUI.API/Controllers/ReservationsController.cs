using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Extensions;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.API.Services;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Models.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(BioscoopDbContext context, QrCodeHelper qrCodeHelper, ITicketPricingService ticketPricingService, IStripeService stripeService) : ControllerBase
{
    private bool IsEmployee() => User.IsInRole(AuthConstants.EmployeeRole);

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> Get()
    {
        var auth0UserId = User.GetAuth0UserId();

        var query = context.Reservations
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Room)
            .Include(r => r.ShowtimeSeats)
                .ThenInclude(ss => ss.Seat)
            .AsQueryable();

        if (!IsEmployee())
            query = query.Where(r => r.Auth0UserId == auth0UserId);

        var reservations = await query.ToListAsync();
        var response = reservations.Select(r =>
        {
            var seats = r.ShowtimeSeats
                .Select(ss => new SeatDto(
                    ss.SeatId,
                    ss.Seat.Row,
                    ss.Seat.SeatNumber))
                .ToList();

            return new ReservationResponseDto(
                r.Id,
                new ShowtimeResponseDto(
                    r.Showtime.Id,
                    r.Showtime.MovieId,
                    r.Showtime.RoomId,
                    r.Showtime.Room.Name,
                    r.Showtime.StartTime,
                    0,
                    r.Showtime.DiscountPercentage
                ),
                seats,
                r.Showtime.Movie.Title,
                r.Showtime.Room.Name,
                r.TotalPrice,
                r.Status,
                r.CreatedAt
            );
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ReservationResponseDto>> Get(int id)
    {
        var auth0UserId = User.GetAuth0UserId();

        var query = context.Reservations
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Room)
            .Include(r => r.ShowtimeSeats)
                .ThenInclude(ss => ss.Seat)
            .AsQueryable();

        if (!IsEmployee())
            query = query.Where(r => r.Auth0UserId == auth0UserId);

        var reservation = await query.FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
            return NotFound();

        var seats = reservation.ShowtimeSeats
            .Select(ss => new SeatDto(
                ss.SeatId,
                ss.Seat.Row,
                ss.Seat.SeatNumber))
            .ToList();

        var response = new ReservationResponseDto(
            reservation.Id,
            new ShowtimeResponseDto(
                reservation.Showtime.Id,
                reservation.Showtime.MovieId,
                reservation.Showtime.RoomId,
                reservation.Showtime.Room.Name,
                reservation.Showtime.StartTime,
                0,
                reservation.Showtime.DiscountPercentage
            ),
            seats,
            reservation.Showtime.Movie.Title,
            reservation.Showtime.Room.Name,
            reservation.TotalPrice,
            reservation.Status,
            reservation.CreatedAt
        );

        return Ok(response);
    }

    [HttpPost("validate-qr")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<ActionResult<QrCodeValidationResponseDto>> ValidateQrCode([FromBody] QrCodeValidationRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.QrCode))
            return BadRequest(new QrCodeValidationResponseDto(false, null, "QR code is required"));

        var qrCodeData = qrCodeHelper.ParseQrCode(request.QrCode);

        if (qrCodeData is null)
            return Ok(new QrCodeValidationResponseDto(false, null, "Invalid QR code format"));

        if (!qrCodeHelper.VerifyChecksum(request.QrCode))
            return Ok(new QrCodeValidationResponseDto(false, null, "Invalid QR code checksum"));

        if (qrCodeData.ReservationId is null)
            return Ok(new QrCodeValidationResponseDto(false, null, "Reservation not found"));

        var reservation = await context.Reservations
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Room)
            .Include(r => r.ShowtimeSeats)
                .ThenInclude(ss => ss.Seat)
            .FirstOrDefaultAsync(r => r.Id == qrCodeData.ReservationId.Value);

        if (reservation is null)
            return Ok(new QrCodeValidationResponseDto(false, null, "Reservation not found"));

        if (reservation.Status is ReservationStatus.Cancelled)
            return Ok(new QrCodeValidationResponseDto(false, null, "This reservation has been cancelled."));

        if (qrCodeData.ShowtimeId != reservation.ShowtimeId || qrCodeData.RoomId != reservation.Showtime.RoomId)
            return Ok(new QrCodeValidationResponseDto(false, reservation.Id, "This ticket does not match the showtime."));

        var reservationSeatIds = reservation.ShowtimeSeats.Select(showtimeSeat => showtimeSeat.SeatId).ToHashSet();
        if (qrCodeData.SeatIds is null || qrCodeData.SeatIds.Any(seatId => !reservationSeatIds.Contains(seatId)))
            return Ok(new QrCodeValidationResponseDto(false, reservation.Id, "This ticket does not belong to the reservation."));

        var now = DateTime.Now;
        if (now < reservation.Showtime.StartTime.AddHours(-2))
            return Ok(new QrCodeValidationResponseDto(false, reservation.Id, "This ticket is not valid yet."));

        if (now >= reservation.Showtime.StartTime)
            return Ok(new QrCodeValidationResponseDto(false, reservation.Id, "This ticket is no longer valid because the movie has already started."));

        var scannedSeatIds = qrCodeData.SeatIds.ToHashSet();
        var seats = reservation.ShowtimeSeats
            .Where(showtimeSeat => scannedSeatIds.Contains(showtimeSeat.SeatId))
            .Select(showtimeSeat => new SeatDto(
                showtimeSeat.SeatId,
                showtimeSeat.Seat.Row,
                showtimeSeat.Seat.SeatNumber))
            .OrderBy(seat => seat.Row)
            .ThenBy(seat => seat.SeatNumber)
            .ToList();

        return Ok(new QrCodeValidationResponseDto(
            true,
            reservation.Id,
            null,
            reservation.Showtime.Movie.Title,
            reservation.Showtime.Room.Name,
            reservation.Showtime.StartTime,
            seats));
    }

    [HttpPost("{showtimeId:int}/reserve")]
    [Authorize]
    public async Task<ActionResult<ReservationConfirmResponseDto>> ConfirmReservation(
        int showtimeId,
        [FromBody] ReservationConfirmRequestDto request)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        switch (request.SeatIds.Count)
        {
            case 0:
                return BadRequest("No seats selected");
            case > 20:
                return BadRequest("Maximum 20 seats per reservation");
        }

        var showtime = await context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r.Rows)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        var seats = await context.Seats
            .Where(s => request.SeatIds.Contains(s.Id) && s.RoomId == showtime.RoomId)
            .ToListAsync();

        if (seats.Count != request.SeatIds.Count)
            return BadRequest("One or more selected seats do not exist or belong to a different room");

        var existingReservations = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtimeId && request.SeatIds.Contains(ss.SeatId) && ss.ReservationId.HasValue)
            .ToListAsync();

        if (existingReservations.Any())
            return BadRequest("One or more selected seats are already reserved");

        var reservation = new Reservation
        {
            ShowtimeId = showtimeId,
            Auth0UserId = auth0UserId,
            TotalPrice = (await ticketPricingService.GetPriceQuoteAsync(showtime, request.SeatIds.Count)).TotalPrice,
            Status = ReservationStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        if (request.PopcornOrders is { Count: > 0 })
        {
            foreach (var popcorn in request.PopcornOrders)
            {
                reservation.PopcornOrders.Add(new PopcornOrder
                {
                    Size = popcorn.Size,
                    Flavor = popcorn.Flavor,
                    AddDrink = popcorn.AddDrink,
                    AddRefill = popcorn.AddRefill
                });
            }
        }

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync();

        var existingShowtimeSeats = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtimeId && request.SeatIds.Contains(ss.SeatId))
            .ToListAsync();

        foreach (var showtimeSeat in existingShowtimeSeats)
            showtimeSeat.ReservationId = reservation.Id;

        await context.SaveChangesAsync();

        var reservedSeatInfos = seats.Select(seat => new SeatInfoDto(
            seat.Id,
            seat.Row,
            seat.SeatNumber,
            false,
            SeatSelectionService.CalculateSeatScore(seat.Row, seat.SeatNumber,
                showtime.Room.Rows.Count, showtime.Room.Rows.Max(r => r.SeatCount))
        )).ToList();

        var response = new ReservationConfirmResponseDto(
            reservation.Id,
            reservedSeatInfos,
            $"Successfully reserved {request.SeatIds.Count} seat(s) for showtime {showtimeId}"
        );

        return Ok(response);
    }

    [HttpPut("{id:int}/seats")]
    [Authorize]
    public async Task<IActionResult> ChangeSeats(int id, [FromBody] ReservationSeatChangeRequestDto request)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var reservation = await context.Reservations
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Room)
            .Include(r => r.ShowtimeSeats)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
            return NotFound();

        if (!IsEmployee() && reservation.Auth0UserId != auth0UserId)
            return NotFound();

        if (reservation.Status is ReservationStatus.Cancelled)
            return BadRequest("Cancelled reservations cannot be changed");

        var currentSeatCount = reservation.ShowtimeSeats.Count;
        if (request.SeatIds.Count != currentSeatCount)
            return BadRequest("The number of seats cannot be changed");

        var uniqueSeatIds = request.SeatIds.Distinct().ToList();
        if (uniqueSeatIds.Count != request.SeatIds.Count)
            return BadRequest("Duplicate seats selected");

        var selectedSeats = await context.Seats
            .Where(s => uniqueSeatIds.Contains(s.Id) && s.RoomId == reservation.Showtime.RoomId)
            .ToListAsync();

        if (selectedSeats.Count != uniqueSeatIds.Count)
            return BadRequest("One or more selected seats do not exist or belong to a different room");

        var now = DateTime.UtcNow;
        var targetShowtimeSeats = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == reservation.ShowtimeId && uniqueSeatIds.Contains(ss.SeatId))
            .ToListAsync();

        var hasUnavailableSeat = targetShowtimeSeats.Any(ss =>
            (ss.ReservationId.HasValue && ss.ReservationId != reservation.Id)
            || (ss.HoldId.HasValue && ss.HoldExpiresAtUtc > now && ss.HoldAuth0UserId != auth0UserId));

        if (hasUnavailableSeat)
            return BadRequest("One or more selected seats are already reserved or held");

        foreach (var currentShowtimeSeat in reservation.ShowtimeSeats)
        {
            currentShowtimeSeat.ReservationId = null;
            currentShowtimeSeat.HoldId = null;
            currentShowtimeSeat.HoldAuth0UserId = null;
            currentShowtimeSeat.HoldExpiresAtUtc = null;
        }

        foreach (var targetShowtimeSeat in targetShowtimeSeats)
        {
            targetShowtimeSeat.ReservationId = reservation.Id;
            targetShowtimeSeat.HoldId = null;
            targetShowtimeSeat.HoldAuth0UserId = null;
            targetShowtimeSeat.HoldExpiresAtUtc = null;
        }

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var reservation = await context.Reservations
            .Include(r => r.ShowtimeSeats)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation is null)
            return NotFound();

        if (!IsEmployee() && reservation.Auth0UserId != auth0UserId)
            return NotFound();

        if (reservation.Status is ReservationStatus.Cancelled)
            return NoContent();

        if (!string.IsNullOrWhiteSpace(reservation.StripePaymentIntentId))
            await stripeService.RefundPaymentAsync(reservation.StripePaymentIntentId);

        reservation.Status = ReservationStatus.Cancelled;

        foreach (var showtimeSeat in reservation.ShowtimeSeats)
        {
            showtimeSeat.ReservationId = null;
            showtimeSeat.HoldId = null;
            showtimeSeat.HoldAuth0UserId = null;
            showtimeSeat.HoldExpiresAtUtc = null;
        }

        await context.SaveChangesAsync();
        return NoContent();
    }
}