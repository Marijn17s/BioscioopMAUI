using BioscoopCasus.API.Resources;
using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShowtimesController(BioscoopDbContext context, IStringLocalizer<SharedResource> localizer) : ControllerBase
{
    // GET /api/showtimes
    // Optionally pass ?movieId=... or ?date=...
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShowtimeResponseDto>>> GetShowtimes(
        [FromQuery] int? movieId,
        [FromQuery] DateTime? date)
    {
        var query = context.Showtimes.AsQueryable();

        if (movieId.HasValue)
        {
            query = query.Where(s => s.MovieId == movieId.Value);
        }

        if (date.HasValue)
        {
            var targetDate = date.Value.Date;
            query = query.Where(s => s.StartTime.Date == targetDate);
        }

        var showtimes = await query
            .OrderBy(s => s.StartTime)
            .Select(s => new ShowtimeResponseDto(
                s.Id,
                s.MovieId,
                s.RoomId,
                s.Room.Name,
                s.StartTime,
                0,
                s.DiscountPercentage
            ))
            .ToListAsync();

        return Ok(showtimes);
    }

    // POST /api/showtimes
    [HttpPost]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<ActionResult<ShowtimeResponseDto>> CreateShowtime(ShowtimeCreateDto dto)
    {
        if (!await context.Movies.AnyAsync(m => m.Id == dto.MovieId))
            return BadRequest(localizer["InvalidMovieId"].Value);

        var movie = await context.Movies.FindAsync(dto.MovieId);
        if (movie is null)
            return BadRequest(localizer["InvalidMovieId"].Value);

        if (!await context.Rooms.AnyAsync(r => r.Id == dto.RoomId))
            return BadRequest(localizer["InvalidRoomId"].Value);

        if (dto.StartTime < DateTime.Now)
            return BadRequest(localizer["ShowtimeInPast"].Value);

        if (dto.DiscountPercentage < 0 || dto.DiscountPercentage > 100)
            return BadRequest("Discount percentage must be between 0 and 100.");

        // Calculate end time (duration + 30 min cleaning buffer)
        var newEndTime = dto.StartTime.AddMinutes(movie.DurationMinutes + 30);

        // Check for double bookings in the requested room
        var hasConflict = await context.Showtimes
            .Include(s => s.Movie)
            .AnyAsync(s => s.RoomId == dto.RoomId &&
                           ((dto.StartTime >= s.StartTime && dto.StartTime < s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30)) ||
                            (newEndTime > s.StartTime && newEndTime <= s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30)) ||
                            (dto.StartTime <= s.StartTime && newEndTime >= s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30))));

        if (hasConflict)
        {
            return BadRequest(localizer["RoomAlreadyBooked"].Value);
        }

        var showtime = new Showtime
        {
            MovieId = dto.MovieId,
            RoomId = dto.RoomId,
            StartTime = dto.StartTime,
            DiscountPercentage = dto.DiscountPercentage
        };

        context.Showtimes.Add(showtime);
        await context.SaveChangesAsync();

        var roomName = await context.Rooms
            .Where(room => room.Id == showtime.RoomId)
            .Select(room => room.Name)
            .FirstAsync();

        var response = new ShowtimeResponseDto(
            showtime.Id,
            showtime.MovieId,
            showtime.RoomId,
            roomName,
            showtime.StartTime,
            0,
            showtime.DiscountPercentage
        );

        return CreatedAtAction(nameof(GetShowtimes), new { id = showtime.Id }, response);
    }

    // POST /api/showtimes/bulk
    [HttpPost("bulk")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<ActionResult<IEnumerable<ShowtimeResponseDto>>> BulkCreateShowtimes(ShowtimeBulkCreateDto bulkDto)
    {
        if (bulkDto.Showtimes is null || !bulkDto.Showtimes.Any())
            return BadRequest("No showtimes provided.");

        var showtimeEntities = new List<Showtime>();
        var responseDtos = new List<ShowtimeResponseDto>();

        // Pre-fetch all movies for duration checks to avoid N+1 queries
        var movieIds = bulkDto.Showtimes.Select(s => s.MovieId).Distinct().ToList();
        var movies = await context.Movies.Where(m => movieIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id);

        // Get the date range for existing showtimes
        var minTime = bulkDto.Showtimes.Min(s => s.StartTime);
        var maxTime = bulkDto.Showtimes.Max(s => s.StartTime.AddMinutes(240)); // rough max duration buffer
        var roomIds = bulkDto.Showtimes.Select(s => s.RoomId).Distinct().ToList();
        var rooms = await context.Rooms
            .Where(room => roomIds.Contains(room.Id))
            .ToDictionaryAsync(room => room.Id);

        // Fetch existing showtimes in that timeframe
        var existingShowtimes = await context.Showtimes
            .Include(s => s.Movie)
            .Where(s => roomIds.Contains(s.RoomId) && s.StartTime >= minTime.AddDays(-1) && s.StartTime <= maxTime.AddDays(1))
            .ToListAsync();

        foreach (var dto in bulkDto.Showtimes)
        {
            if (!movies.TryGetValue(dto.MovieId, out var movie))
                return BadRequest(localizer["InvalidMovieId"].Value);

            var newEndTime = dto.StartTime.AddMinutes(movie.DurationMinutes + 30); // 30 min cleaning

            // Check against existing database showtimes
            var dbConflict = existingShowtimes.Any(s =>
                s.RoomId == dto.RoomId &&
                ((dto.StartTime >= s.StartTime && dto.StartTime < s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30)) ||
                 (newEndTime > s.StartTime && newEndTime <= s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30)) ||
                 (dto.StartTime <= s.StartTime && newEndTime >= s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30))));

            // Check against other showtimes currently being processed in this bulk request
            var inMemoryConflict = showtimeEntities.Any(s =>
            {
                var processingMovie = movies[s.MovieId];
                var processingEndTime = s.StartTime.AddMinutes(processingMovie.DurationMinutes + 30);
                return s.RoomId == dto.RoomId &&
                       ((dto.StartTime >= s.StartTime && dto.StartTime < processingEndTime) ||
                        (newEndTime > s.StartTime && newEndTime <= processingEndTime) ||
                        (dto.StartTime <= s.StartTime && newEndTime >= processingEndTime));
            });

            if (dto.StartTime < DateTime.Now)
            {
                return BadRequest(localizer["ShowtimeInPast"].Value);
            }

            if (dto.DiscountPercentage < 0 || dto.DiscountPercentage > 100)
                return BadRequest("Discount percentage must be between 0 and 100.");

            if (dbConflict || inMemoryConflict)
            {
                return BadRequest(localizer["RoomSchedulingConflict", dto.RoomId, dto.StartTime].Value);
            }

            var showtime = new Showtime
            {
                MovieId = dto.MovieId,
                RoomId = dto.RoomId,
                StartTime = dto.StartTime,
                DiscountPercentage = dto.DiscountPercentage
            };

            showtimeEntities.Add(showtime);

            responseDtos.Add(new ShowtimeResponseDto(
                showtime.Id,
                showtime.MovieId,
                showtime.RoomId,
                rooms[dto.RoomId].Name,
                showtime.StartTime,
                0,
                showtime.DiscountPercentage
            ));
        }

        await context.Showtimes.AddRangeAsync(showtimeEntities);
        await context.SaveChangesAsync();

        for (var index = 0; index < showtimeEntities.Count; index++)
        {
            var savedShowtime = showtimeEntities[index];
            var dto = bulkDto.Showtimes[index];
            responseDtos[index] = new ShowtimeResponseDto(
                savedShowtime.Id,
                savedShowtime.MovieId,
                savedShowtime.RoomId,
                rooms[dto.RoomId].Name,
                savedShowtime.StartTime,
                0,
                savedShowtime.DiscountPercentage);
        }

        return Ok(responseDtos);
    }

    // PUT /api/showtimes/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<ActionResult<ShowtimeResponseDto>> UpdateShowtime(int id, ShowtimeCreateDto dto)
    {
        var showtime = await context.Showtimes.FindAsync(id);
        if (showtime is null)
        {
            return NotFound("Show not found");
        }

        var movie = await context.Movies.FindAsync(dto.MovieId);
        if (movie is null)
            return BadRequest(localizer["InvalidMovieId"].Value);

        if (!await context.Rooms.AnyAsync(r => r.Id == dto.RoomId))
            return BadRequest(localizer["InvalidRoomId"].Value);

        if (dto.StartTime < DateTime.Now)
            return BadRequest(localizer["ShowtimeInPast"].Value);

        if (dto.DiscountPercentage < 0 || dto.DiscountPercentage > 100)
            return BadRequest("Discount percentage must be between 0 and 100.");

        // Calculate end time (duration + 30 min cleaning buffer)
        var newEndTime = dto.StartTime.AddMinutes(movie.DurationMinutes + 30);

        // Check for double bookings excluding the current showtime
        var hasConflict = await context.Showtimes
            .Include(s => s.Movie)
            .AnyAsync(s => s.Id != id && s.RoomId == dto.RoomId &&
                           ((dto.StartTime >= s.StartTime && dto.StartTime < s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30)) ||
                            (newEndTime > s.StartTime && newEndTime <= s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30)) ||
                            (dto.StartTime <= s.StartTime && newEndTime >= s.StartTime.AddMinutes(s.Movie.DurationMinutes + 30))));

        if (hasConflict)
        {
            return BadRequest(localizer["RoomAlreadyBooked"].Value);
        }

        showtime.MovieId = dto.MovieId;
        showtime.RoomId = dto.RoomId;
        showtime.StartTime = dto.StartTime;
        showtime.DiscountPercentage = dto.DiscountPercentage;

        await context.SaveChangesAsync();

        var roomName = await context.Rooms
            .Where(room => room.Id == showtime.RoomId)
            .Select(room => room.Name)
            .FirstAsync();

        return Ok(new ShowtimeResponseDto(
            showtime.Id,
            showtime.MovieId,
            showtime.RoomId,
            roomName,
            showtime.StartTime,
            0,
            showtime.DiscountPercentage
        ));
    }

    // DELETE /api/showtimes/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<IActionResult> DeleteShowtime(int id)
    {
        var showtime = await context.Showtimes.FindAsync(id);
        if (showtime is null)
            return NotFound();

        context.Showtimes.Remove(showtime);
        await context.SaveChangesAsync();

        return NoContent();
    }
}