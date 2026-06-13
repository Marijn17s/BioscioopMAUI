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
[Route("api/seat-selection")]
public class SeatSelectionController(BioscoopDbContext context, ITicketPricingService ticketPricingService) : ControllerBase
{
    [HttpGet("{showtimeId}/available")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<SeatInfoDto>>> GetAvailableSeats(int showtimeId)
    {
        var showtime = await context.Showtimes
            .Include(s => s.Room)
            .ThenInclude(r => r.Rows)
            .Include(s => s.Room)
            .ThenInclude(r => r.Seats)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        await EnsureSeatsInitializedForShowtime(showtime);
        await ClearExpiredHoldsAsync(showtimeId);

        var now = DateTime.UtcNow;
        var occupiedSeatIds = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtimeId
                && (ss.ReservationId.HasValue || (ss.HoldId.HasValue && ss.HoldExpiresAtUtc > now)))
            .Select(ss => ss.SeatId)
            .ToHashSetAsync();

        var seats = await context.Seats
            .Where(s => s.RoomId == showtime.RoomId)
            .OrderBy(s => s.Row)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();

        var totalRows = showtime.Room.Rows.Any()
            ? showtime.Room.Rows.Count
            : seats.Select(s => s.Row).Distinct().Count();
        var maxSeatsPerRow = showtime.Room.Rows.Any()
            ? showtime.Room.Rows.Max(r => r.SeatCount)
            : seats.GroupBy(s => s.Row).Select(g => g.Count()).DefaultIfEmpty(0).Max();

        var seatInfos = seats.Select(seat => new SeatInfoDto(
            seat.Id,
            seat.Row,
            seat.SeatNumber,
            !occupiedSeatIds.Contains(seat.Id),
            SeatSelectionService.CalculateSeatScore(seat.Row, seat.SeatNumber, totalRows, maxSeatsPerRow)
        )).ToList();

        return Ok(seatInfos);
    }
    
    [HttpPost("{showtimeId}/suggest")]
    [Authorize]
    public async Task<ActionResult<SeatSelectionResponseDto>> SuggestSeats(
        int showtimeId,
        [FromBody] SeatSelectionRequestDto request)
    {
        if (request.GroupSize <= 0 || request.GroupSize > 20)
            return BadRequest("Group size must be between 1 and 20");

        var showtime = await context.Showtimes
            .Include(s => s.Room)
            .ThenInclude(r => r.Rows)
            .Include(s => s.Room)
            .ThenInclude(r => r.Seats)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        await EnsureSeatsInitializedForShowtime(showtime);
        await ClearExpiredHoldsAsync(showtimeId);

        var showtimeSeatsQuery = context.ShowtimeSeats.Where(ss => ss.ShowtimeId == showtimeId);

        var result = SeatSelectionService.SelectBestSeats(showtime, request.GroupSize, showtimeSeatsQuery);

        var suggestedSeatInfos = result.SelectedSeats.Select(seat => new SeatInfoDto(
            seat.Id,
            seat.Row,
            seat.SeatNumber,
            true,
            SeatSelectionService.CalculateSeatScore(seat.Row, seat.SeatNumber,
                showtime.Room.Rows.Count, showtime.Room.Rows.Max(r => r.SeatCount))
        )).ToList();

        var groupedSeatInfos = result.GroupedSeats.Select(group =>
            group.Select(seat => new SeatInfoDto(
                seat.Id,
                seat.Row,
                seat.SeatNumber,
                true,
                SeatSelectionService.CalculateSeatScore(seat.Row, seat.SeatNumber,
                    showtime.Room.Rows.Count, showtime.Room.Rows.Max(r => r.SeatCount))
            )).ToList()
        ).ToList();

        var response = new SeatSelectionResponseDto(
            suggestedSeatInfos,
            result.Message,
            result.IsGroupedTogether,
            groupedSeatInfos,
            result.TotalScore
        );

        return Ok(response);
    }

    [HttpGet("{showtimeId:int}/price-quote")]
    [Authorize]
    public async Task<ActionResult<PriceQuoteDto>> GetPriceQuote(int showtimeId, [FromQuery] int seatCount)
    {
        if (seatCount <= 0 || seatCount > 20)
            return BadRequest("Seat count must be between 1 and 20");

        var showtime = await context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        return Ok(await ticketPricingService.GetPriceQuoteAsync(showtime, seatCount));
    }

    [HttpPost("{showtimeId:int}/holds")]
    [Authorize]
    public async Task<ActionResult<CreateSeatHoldResponseDto>> CreateHold(
        int showtimeId,
        [FromBody] CreateSeatHoldRequestDto request)
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

        var uniqueSeatIds = request.SeatIds.Distinct().ToList();
        if (uniqueSeatIds.Count != request.SeatIds.Count)
            return BadRequest("Duplicate seats selected");

        var showtime = await context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .ThenInclude(r => r.Rows)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        await EnsureSeatsInitializedForShowtime(showtime);
        await ClearExpiredHoldsAsync(showtimeId);

        var seats = await context.Seats
            .Where(s => uniqueSeatIds.Contains(s.Id) && s.RoomId == showtime.RoomId)
            .ToListAsync();

        if (seats.Count != uniqueSeatIds.Count)
            return BadRequest("One or more selected seats do not exist or belong to a different room");

        var now = DateTime.UtcNow;
        var selectedShowtimeSeats = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtimeId && uniqueSeatIds.Contains(ss.SeatId))
            .ToListAsync();

        if (selectedShowtimeSeats.Count != uniqueSeatIds.Count)
            return BadRequest("One or more selected seats are not available for this showtime");

        if (selectedShowtimeSeats.Any(ss => ss.ReservationId.HasValue || (ss.HoldId.HasValue && ss.HoldExpiresAtUtc > now)))
            return BadRequest("One or more selected seats are already reserved or held");

        var holdId = Guid.NewGuid();
        var expiresAtUtc = now.AddMinutes(10);

        foreach (var showtimeSeat in selectedShowtimeSeats)
        {
            showtimeSeat.HoldId = holdId;
            showtimeSeat.HoldAuth0UserId = auth0UserId;
            showtimeSeat.HoldExpiresAtUtc = expiresAtUtc;
        }

        await context.SaveChangesAsync();

        var priceQuote = await ticketPricingService.GetPriceQuoteAsync(showtime, uniqueSeatIds.Count);
        return Ok(new CreateSeatHoldResponseDto
        {
            HoldId = holdId,
            ExpiresAtUtc = expiresAtUtc,
            PriceQuote = priceQuote
        });
    }

    [HttpDelete("holds/{holdId:guid}")]
    [Authorize]
    public async Task<IActionResult> ReleaseHold(Guid holdId)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var heldSeats = await context.ShowtimeSeats
            .Where(ss => ss.HoldId == holdId && ss.HoldAuth0UserId == auth0UserId && !ss.ReservationId.HasValue)
            .ToListAsync();

        foreach (var showtimeSeat in heldSeats)
        {
            showtimeSeat.HoldId = null;
            showtimeSeat.HoldAuth0UserId = null;
            showtimeSeat.HoldExpiresAtUtc = null;
        }

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{showtimeId:int}/initialize-seats")]
    [Authorize]
    public async Task<ActionResult<string>> InitializeSeatsForShowtime(int showtimeId)
    {
        var showtime = await context.Showtimes
            .Include(s => s.Room)
            .ThenInclude(r => r.Seats)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        var existingShowtimeSeatsCount = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtimeId)
            .CountAsync();

        if (existingShowtimeSeatsCount > 0)
            return Ok($"Seats already initialized for showtime {showtimeId} ({existingShowtimeSeatsCount} seats)");

        var roomSeats = showtime.Room.Seats.ToList();

        if (!roomSeats.Any())
            return BadRequest($"No seats found for room {showtime.RoomId}");

        var showtimeSeats = roomSeats.Select(seat => new ShowtimeSeat
        {
            ShowtimeId = showtimeId,
            SeatId = seat.Id,
            ReservationId = null
        }).ToList();

        context.ShowtimeSeats.AddRange(showtimeSeats);
        await context.SaveChangesAsync();

        return Ok($"Successfully initialized {showtimeSeats.Count} seats for showtime {showtimeId} in room {showtime.Room.Name}");
    }

    [HttpGet("debug/{showtimeId:int}")]
    [Authorize]
    public async Task<ActionResult<object>> DebugShowtime(int showtimeId)
    {
        var showtime = await context.Showtimes
            .Include(s => s.Room)
            .ThenInclude(r => r.Seats)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null)
            return NotFound("Showtime not found");

        var showtimeSeats = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtimeId)
            .Include(ss => ss.Seat)
            .ToListAsync();

        var reservations = await context.Reservations
            .Where(r => r.ShowtimeId == showtimeId)
            .ToListAsync();

        return Ok(new
        {
            Showtime = new { showtime.Id, showtime.StartTime, RoomName = showtime.Room.Name },
            TotalSeatsInRoom = showtime.Room.Seats.Count,
            ShowtimeSeatsCount = showtimeSeats.Count,
            OccupiedSeatsCount = showtimeSeats.Count(ss => ss.ReservationId.HasValue),
            ReservationsCount = reservations.Count,
            OccupiedSeats = showtimeSeats
                .Where(ss => ss.ReservationId.HasValue)
                .Select(ss => new { ss.Seat.Row, ss.Seat.SeatNumber, ss.ReservationId })
                .ToList()
        });
    }

    private async Task EnsureSeatsInitializedForShowtime(Showtime showtime)
    {
        var existingShowtimeSeatsCount = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtime.Id)
            .CountAsync();

        if (existingShowtimeSeatsCount > 0)
            return;

        var roomSeats = await context.Seats
            .Where(s => s.RoomId == showtime.RoomId)
            .ToListAsync();

        if (!roomSeats.Any())
        {
            var roomRows = await context.Rows
                .Where(r => r.RoomId == showtime.RoomId)
                .OrderBy(r => r.RowNumber)
                .ToListAsync();

            if (!roomRows.Any())
                throw new InvalidOperationException($"No rows found for room {showtime.RoomId}");

            roomSeats = roomRows
                .SelectMany(row => Enumerable.Range(1, row.SeatCount).Select(seatNumber => new Seat
                {
                    RoomId = showtime.RoomId,
                    Row = row.RowNumber,
                    SeatNumber = seatNumber
                }))
                .ToList();

            context.Seats.AddRange(roomSeats);
            await context.SaveChangesAsync();
        }

        var showtimeSeats = roomSeats.Select(seat => new ShowtimeSeat
        {
            ShowtimeId = showtime.Id,
            SeatId = seat.Id,
            ReservationId = null
        }).ToList();

        context.ShowtimeSeats.AddRange(showtimeSeats);
        await context.SaveChangesAsync();
    }

    private async Task ClearExpiredHoldsAsync(int showtimeId)
    {
        var now = DateTime.UtcNow;
        var expiredHolds = await context.ShowtimeSeats
            .Where(ss => ss.ShowtimeId == showtimeId && ss.HoldId.HasValue && ss.HoldExpiresAtUtc <= now && !ss.ReservationId.HasValue)
            .ToListAsync();

        foreach (var showtimeSeat in expiredHolds)
        {
            showtimeSeat.HoldId = null;
            showtimeSeat.HoldAuth0UserId = null;
            showtimeSeat.HoldExpiresAtUtc = null;
        }

        if (expiredHolds.Count > 0)
            await context.SaveChangesAsync();
    }
}