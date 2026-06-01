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
public class ReservationsController(BioscoopDbContext context, QrCodeHelper qrCodeHelper) : ControllerBase
{
    private bool IsEmployee() => User.IsInRole(AuthConstants.EmployeeRole);

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> Get()
    {
        var query = context.Reservations
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Room)
            .Include(r => r.ShowtimeSeats)
                .ThenInclude(ss => ss.Seat)
            .AsQueryable();

        if (!IsEmployee())
            query = query.Where(r => r.Auth0UserId == User.GetAuth0UserId());

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
                    0
                ),
                seats,
                r.Showtime.Movie.Title,
                r.Showtime.Room.Name
            );
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<ReservationResponseDto>> Get(int id)
    {
        var query = context.Reservations
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Room)
            .Include(r => r.ShowtimeSeats)
                .ThenInclude(ss => ss.Seat)
            .AsQueryable();

        if (!IsEmployee())
            query = query.Where(r => r.Auth0UserId == User.GetAuth0UserId());

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
                0
            ),
            seats,
            reservation.Showtime.Movie.Title,
            reservation.Showtime.Room.Name
        );

        return Ok(response);
    }

    [HttpPost("validate-qr")]
    [Authorize]
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

        var query = context.Reservations
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Movie)
            .Include(r => r.Showtime)
                .ThenInclude(s => s.Room)
            .Include(r => r.ShowtimeSeats)
                .ThenInclude(ss => ss.Seat)
            .AsQueryable();

        if (!IsEmployee())
            query = query.Where(r => r.Auth0UserId == User.GetAuth0UserId());

        var reservation = await query.FirstOrDefaultAsync(r => r.Id == qrCodeData.ReservationId.Value);

        if (reservation is null)
            return Ok(new QrCodeValidationResponseDto(false, null, "Reservation not found"));

        if (reservation.Showtime.StartTime <= DateTime.UtcNow)
            return Ok(new QrCodeValidationResponseDto(false, null, "This ticket can no longer be viewed because the movie has already started."));

        return Ok(new QrCodeValidationResponseDto(true, reservation.Id, null));
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
            .Include(s => s.Room)
            .ThenInclude(r => r.Rows)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime == null)
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
            TotalPrice = request.TotalPrice
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
}