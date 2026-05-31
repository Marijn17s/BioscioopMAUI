using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController(BioscoopDbContext context) : ControllerBase
{
    // GET /api/movies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieResponseDto>>> GetMovies()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        
        var movies = await context.Movies
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();

        var result = movies.Select(m => 
        {
            return new MovieResponseDto(
                m.Id,
                m.Title,
                m.Description,
                m.PosterUrl,
                m.Actors,
                m.TrailerUrl,
                m.Genres,
                m.AgeRating,
                m.DurationMinutes,
                m.ReleaseDate,
                null
            );
        }).ToList();

        return Ok(result);
    }

    // GET /api/movies/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<MovieResponseDto>> GetMovie(int id)
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        var movie = await context.Movies
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.Room)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            return NotFound();

        var showtimeDtos = movie.Showtimes.Select(s => new ShowtimeResponseDto(
            s.Id,
            s.MovieId,
            s.RoomId,
            s.Room.Name,
            s.StartTime,
            0 // Ticket price is out of scope for now
        )).OrderBy(s => s.StartTime).ToList();

        var response = new MovieResponseDto(
            movie.Id,
            movie.Title,
            movie.Description,
            movie.PosterUrl,
            movie.Actors,
            movie.TrailerUrl,
            movie.Genres,
            movie.AgeRating,
            movie.DurationMinutes,
            movie.ReleaseDate,
            showtimeDtos
        );

        return Ok(response);
    }

    // POST /api/movies
    [HttpPost]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<ActionResult<MovieResponseDto>> CreateMovie(MovieCreateDto dto)
    {
        var movie = new Movie
        {
            Title = dto.Title,
            PosterUrl = dto.PosterUrl,
            Actors = dto.Actors,
            TrailerUrl = dto.TrailerUrl,
            AgeRating = dto.AgeRating,
            DurationMinutes = dto.DurationMinutes,
            ReleaseDate = dto.ReleaseDate
        };

        context.Movies.Add(movie);
        await context.SaveChangesAsync();

        var response = new MovieResponseDto(
            movie.Id,
            dto.Title,
            dto.Description,
            movie.PosterUrl,
            movie.Actors,
            movie.TrailerUrl,
            dto.Genres,
            movie.AgeRating,
            movie.DurationMinutes,
            movie.ReleaseDate,
            new List<ShowtimeResponseDto>()
        );

        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, response);
    }

    // PUT /api/movies/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<IActionResult> UpdateMovie(int id, MovieUpdateDto dto)
    {
        var movie = await context.Movies
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null)
            return NotFound();

        movie.Title = dto.Title;
        movie.Description = dto.Description;
        movie.PosterUrl = dto.PosterUrl;
        movie.Actors = dto.Actors;
        movie.TrailerUrl = dto.TrailerUrl;
        movie.Genres = dto.Genres;
        movie.AgeRating = dto.AgeRating;
        movie.DurationMinutes = dto.DurationMinutes;
        movie.ReleaseDate = dto.ReleaseDate;

        await context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/movies/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        var movie = await context.Movies.FindAsync(id);
        if (movie == null)
            return NotFound();

        context.Movies.Remove(movie);
        await context.SaveChangesAsync();

        return NoContent();
    }
}