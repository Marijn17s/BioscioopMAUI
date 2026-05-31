using BioscoopMAUI.API.Data;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/films-overview")]
public class FilmsOverviewController(BioscoopDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<FilmsOverviewDto>>> GetFilmsOverview()
    {
        var now = DateTime.Now;
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        var showtimes = await context.Showtimes
            .Include(s => s.Movie)
            .Where(s => s.StartTime >= now)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        var result = showtimes.Select(s => {
            return new FilmsOverviewDto(
                s.Id,
                s.MovieId,
                s.Movie.Title,
                s.Movie.Genres,
                s.Movie.DurationMinutes,
                s.StartTime,
                s.Movie.PosterUrl
            );
        }).ToList();

        return Ok(result);
    }
}