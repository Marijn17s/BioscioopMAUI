using BioscoopMAUI.API.Data;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/movies-overview")]
public class MoviesOverviewController(BioscoopDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MoviesOverviewDto>>> GetMoviesOverview([FromQuery] DateTime? date)
    {
        var filterDate = date?.Date ?? DateTime.Today;

        var movies = await context.Movies
            .Include(m => m.Showtimes)
            .Where(m => m.Showtimes.Any(s => s.StartTime.Date == filterDate))
            .OrderBy(m => m.Showtimes.Where(s => s.StartTime.Date == filterDate).Min(s => s.StartTime))
            .ToListAsync();

        var result = movies.Select(m => {
            return new MoviesOverviewDto(
                m.Id,
                m.Title,
                m.Genres,
                m.PosterUrl,
                m.DurationMinutes,
                m.Showtimes
                    .Where(s => s.StartTime.Date == filterDate)
                    .OrderBy(s => s.StartTime)
                    .Select(s => new MoviesOverviewShowtimeDto(s.Id, s.StartTime))
                    .ToList()
            );
        }).ToList();

        return Ok(result);
}
}