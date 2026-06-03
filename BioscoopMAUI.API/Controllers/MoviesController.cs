using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.API.Extensions;
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
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieResponseDto>>> GetMovies()
    {
        var movies = await context.Movies
            .OrderByDescending(m => m.ReleaseDate)
            .ToListAsync();

        var result = movies.Select(m => new MovieResponseDto(
            m.Id,
            m.Title,
            m.Description,
            m.PosterUrl,
            m.Actors,
            m.TrailerUrl,
            m.Genres,
            m.AgeRating,
            m.DurationMinutes,
            m.ReleaseDate
        )).ToList();

        return Ok(result);
    }

    [HttpGet("favorites")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MovieResponseDto>>> GetFavoriteMovies()
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var movies = await context.FavoriteMovies
            .Where(favorite => favorite.Auth0UserId == auth0UserId)
            .Select(favorite => favorite.Movie)
            .OrderByDescending(movie => movie.ReleaseDate)
            .ToListAsync();

        var result = movies.Select(m => new MovieResponseDto(
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
            null,
            true
        )).ToList();

        return Ok(result);
    }

    [HttpGet("recommendations")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MovieResponseDto>>> GetRecommendations()
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var reservedMovies = await context.Reservations
            .Where(reservation => reservation.Auth0UserId == auth0UserId)
            .Select(reservation => reservation.Showtime.Movie)
            .ToListAsync();

        if (reservedMovies.Count == 0)
            return Ok(Enumerable.Empty<MovieResponseDto>());

        var genreFrequencies = new Dictionary<string, int>();
        foreach (var reservedMovie in reservedMovies)
        {
            foreach (var genre in reservedMovie.Genres.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                genreFrequencies[genre] = genreFrequencies.GetValueOrDefault(genre) + 1;
        }

        var topGenres = genreFrequencies
            .OrderByDescending(genre => genre.Value)
            .Select(genre => genre.Key)
            .ToHashSet();

        var reservedMovieIds = reservedMovies
            .Select(movie => movie.Id)
            .ToHashSet();

        var candidateMovies = await context.Movies
            .Where(movie => !reservedMovieIds.Contains(movie.Id))
            .ToListAsync();

        var recommendedMovies = candidateMovies
            .Select(movie => new
            {
                Movie = movie,
                GenreOverlap = movie.Genres
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Count(genre => topGenres.Contains(genre))
            })
            .Where(scored => scored.GenreOverlap > 0)
            .OrderByDescending(scored => scored.GenreOverlap)
            .ThenByDescending(scored => scored.Movie.ReleaseDate)
            .Take(3)
            .Select(scored => new MovieResponseDto(
                scored.Movie.Id,
                scored.Movie.Title,
                scored.Movie.Description,
                scored.Movie.PosterUrl,
                scored.Movie.Actors,
                scored.Movie.TrailerUrl,
                scored.Movie.Genres,
                scored.Movie.AgeRating,
                scored.Movie.DurationMinutes,
                scored.Movie.ReleaseDate
            ))
            .ToList();

        return Ok(recommendedMovies);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieResponseDto>> GetMovie(int id)
    {
        var movie = await context.Movies
            .Include(m => m.Showtimes)
                .ThenInclude(s => s.Room)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie is null)
            return NotFound();

        var showtimeDtos = movie.Showtimes.Select(s => new ShowtimeResponseDto(
            s.Id,
            s.MovieId,
            s.RoomId,
            s.Room.Name,
            s.StartTime,
            0 // Ticket price is out of scope for now
        )).OrderBy(s => s.StartTime).ToList();

        var isFavorite = false;
        var auth0UserId = User.GetAuth0UserId();
        if (!string.IsNullOrWhiteSpace(auth0UserId))
        {
            isFavorite = await context.FavoriteMovies
                .AnyAsync(favorite => favorite.Auth0UserId == auth0UserId && favorite.MovieId == id);
        }

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
            showtimeDtos,
            isFavorite
        );

        return Ok(response);
    }

    [HttpPut("{id:int}/favorite")]
    [Authorize]
    public async Task<ActionResult<FavoriteMovieStatusDto>> SetFavoriteStatus(int id, [FromBody] SetFavoriteMovieRequestDto request)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var movieExists = await context.Movies.AnyAsync(movie => movie.Id == id);
        if (!movieExists)
            return NotFound();

        var existingFavorite = await context.FavoriteMovies
            .FirstOrDefaultAsync(favorite => favorite.Auth0UserId == auth0UserId && favorite.MovieId == id);

        if (request.IsFavorite)
        {
            if (existingFavorite is not null) return Ok(new FavoriteMovieStatusDto(true));
            
            context.FavoriteMovies.Add(new FavoriteMovie
            {
                Auth0UserId = auth0UserId,
                MovieId = id,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            return Ok(new FavoriteMovieStatusDto(true));
        }

        if (existingFavorite is null) return Ok(new FavoriteMovieStatusDto(false));
        
        context.FavoriteMovies.Remove(existingFavorite);
        await context.SaveChangesAsync();

        return Ok(new FavoriteMovieStatusDto(false));
    }

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

        if (movie is null)
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
        if (movie is null)
            return NotFound();

        context.Movies.Remove(movie);
        await context.SaveChangesAsync();

        return NoContent();
    }
}