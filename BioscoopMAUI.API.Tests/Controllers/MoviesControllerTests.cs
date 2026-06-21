using System.Security.Claims;
using BioscoopMAUI.API.Controllers;
using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BioscoopMAUI.API.Tests.Controllers;

[TestFixture]
public class MoviesControllerTests
{
    private const string TestAuth0UserId = "auth0|test-user";

    [Test]
    public async Task GetRecommendations_ExcludesReservedMoviesAndRanksByGenreOverlap()
    {
        var options = new DbContextOptionsBuilder<BioscoopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new BioscoopDbContext(options);
        SeedRecommendationData(context);

        var controller = new MoviesController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = CreateAuthenticatedUser(TestAuth0UserId)
                }
            }
        };

        var actionResult = await controller.GetRecommendations();
        var okResult = actionResult.Result as OkObjectResult;
        var recommendations = okResult?.Value as IEnumerable<MovieResponseDto>;

        Assert.That(recommendations, Is.Not.Null);

        var recommendedTitles = recommendations.Select(movie => movie.Title).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(recommendedTitles, Has.Count.EqualTo(3));
            Assert.That(recommendedTitles, Does.Not.Contain("The Matrix Resurrections"));
            Assert.That(recommendedTitles[0], Is.EqualTo("Spider-Man: Beyond"));
            Assert.That(recommendedTitles, Does.Not.Contain("Inside Out 3"));
        });
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(string auth0UserId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(AuthConstants.Auth0UserIdClaimType, auth0UserId)
        ], "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static void SeedRecommendationData(BioscoopDbContext context)
    {
        var room = new Room
        {
            Id = 1,
            Number = 1,
            Name = "Room 1",
            Has3D = false,
            IsWheelchairAccessible = true
        };

        var reservedMovie = new Movie
        {
            Id = 1,
            Title = "The Matrix Resurrections",
            Description = "Already seen",
            PosterUrl = "matrix.jpg",
            Actors = "Keanu Reeves",
            TrailerUrl = "matrix.mp4",
            Genres = "Sci-Fi, Action",
            AgeRating = 16,
            DurationMinutes = 148,
            ReleaseDate = new DateTime(2025, 12, 22)
        };

        var newestMatch = new Movie
        {
            Id = 2,
            Title = "Spider-Man: Beyond",
            Description = "Best match",
            PosterUrl = "spiderman.jpg",
            Actors = "Tom Holland",
            TrailerUrl = "spiderman.mp4",
            Genres = "Action, Superhero",
            AgeRating = 12,
            DurationMinutes = 130,
            ReleaseDate = new DateTime(2026, 2, 1)
        };

        var olderMatch = new Movie
        {
            Id = 3,
            Title = "Dune: Part Three",
            Description = "Also relevant",
            PosterUrl = "dune.jpg",
            Actors = "Timothée Chalamet",
            TrailerUrl = "dune.mp4",
            Genres = "Sci-Fi, Adventure",
            AgeRating = 12,
            DurationMinutes = 155,
            ReleaseDate = new DateTime(2026, 1, 15)
        };

        var oldestMatch = new Movie
        {
            Id = 4,
            Title = "The Batman: Gotham Nights",
            Description = "Single overlap",
            PosterUrl = "batman.jpg",
            Actors = "Robert Pattinson",
            TrailerUrl = "batman.mp4",
            Genres = "Action, Crime",
            AgeRating = 16,
            DurationMinutes = 152,
            ReleaseDate = new DateTime(2025, 11, 20)
        };

        var unrelatedNewest = new Movie
        {
            Id = 5,
            Title = "Inside Out 3",
            Description = "No overlap",
            PosterUrl = "insideout.jpg",
            Actors = "Amy Poehler",
            TrailerUrl = "insideout.mp4",
            Genres = "Animation, Family",
            AgeRating = 6,
            DurationMinutes = 105,
            ReleaseDate = new DateTime(2026, 2, 14)
        };

        var unrelatedMovie = new Movie
        {
            Id = 6,
            Title = "Frozen III",
            Description = "No overlap",
            PosterUrl = "frozen.jpg",
            Actors = "Idina Menzel",
            TrailerUrl = "frozen.mp4",
            Genres = "Animation, Musical",
            AgeRating = 6,
            DurationMinutes = 110,
            ReleaseDate = new DateTime(2025, 12, 6)
        };

        var showtime = new Showtime
        {
            Id = 1,
            MovieId = reservedMovie.Id,
            RoomId = room.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            Movie = reservedMovie,
            Room = room
        };

        context.Rooms.Add(room);
        context.Movies.AddRange(reservedMovie, newestMatch, olderMatch, oldestMatch, unrelatedNewest, unrelatedMovie);
        context.Showtimes.Add(showtime);
        context.Reservations.Add(new Reservation
        {
            Id = 1,
            ShowtimeId = showtime.Id,
            Auth0UserId = TestAuth0UserId,
            TotalPrice = 17.00m,
            Status = ReservationStatus.Confirmed,
            Showtime = showtime
        });

        context.SaveChanges();
    }
}