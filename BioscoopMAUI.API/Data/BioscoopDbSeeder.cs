using BioscoopMAUI.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Data;

public static class BioscoopDbSeeder
{
    public static async Task SeedAsync(BioscoopDbContext context)
    {
        // Only seed if the database is empty
        if (await context.Movies.AnyAsync())
            return;

        var rooms = CreateRooms();
        var rows = CreateRows(rooms);
        var movies = CreateMovies();
        var showtimes = CreateShowtimes(movies, rooms);
        var seats = CreateSeats(rows);
        var showtimeSeats = CreateShowtimeSeats(showtimes, seats);

        var pinCards = CreatePinCards();

        context.Rooms.AddRange(rooms);
        context.Rows.AddRange(rows);
        context.Movies.AddRange(movies);
        context.Showtimes.AddRange(showtimes);
        context.PinCards.AddRange(pinCards);
        context.Seats.AddRange(seats);
        context.ShowtimeSeats.AddRange(showtimeSeats);

        await context.SaveChangesAsync();
    }

    private static List<Room> CreateRooms()
    {
        return
        [
            new Room { Number = 1, Name = "Room 1", Has3D = true,  IsWheelchairAccessible = true },
            new Room { Number = 2, Name = "Room 2", Has3D = true,  IsWheelchairAccessible = true },
            new Room { Number = 3, Name = "Room 3", Has3D = false, IsWheelchairAccessible = true },
            new Room { Number = 4, Name = "Room 4", Has3D = false, IsWheelchairAccessible = true },
            new Room { Number = 5, Name = "Room 5", Has3D = false, IsWheelchairAccessible = false },
            new Room { Number = 6, Name = "Room 6", Has3D = false, IsWheelchairAccessible = false },
        ];
    }

    private static List<Row> CreateRows(List<Room> rooms)
    {
        var rows = new List<Row>();

        foreach (var room in rooms)
        {
            switch (room.Number)
            {
                // Rooms 1-3: 8 rows of 15 seats = 120 seats
                case 1 or 2 or 3:
                    for (int i = 1; i <= 8; i++)
                        rows.Add(new Row { Room = room, RowNumber = i, SeatCount = 15 });
                    break;

                // Room 4: 6 rows of 10 seats = 60 seats
                case 4:
                    for (int i = 1; i <= 6; i++)
                        rows.Add(new Row { Room = room, RowNumber = i, SeatCount = 10 });
                    break;

                // Rooms 5-6: front 2 rows of 10, back 2 rows of 15 = 50 seats
                case 5 or 6:
                    rows.Add(new Row { Room = room, RowNumber = 1, SeatCount = 10 });
                    rows.Add(new Row { Room = room, RowNumber = 2, SeatCount = 10 });
                    rows.Add(new Row { Room = room, RowNumber = 3, SeatCount = 15 });
                    rows.Add(new Row { Room = room, RowNumber = 4, SeatCount = 15 });
                    break;
            }
        }

        return rows;
    }

    private static List<Movie> CreateMovies()
    {
        return
        [
            new Movie
            {
                Title = "The Matrix Resurrections",
                Description = "Return to the world of two realities: one, everyday life; the other, what lies behind it.",
                PosterUrl = "https://static.wikia.nocookie.net/matrix/images/b/bd/The_Matrix_Resurrections_digital_release_cover.jpg/revision/latest?cb=20220218002244",
                Actors = "Keanu Reeves, Carrie-Anne Moss, Yahya Abdul-Mateen II",
                TrailerUrl = "https://youtube.com/watch?v=9ix7TUGVYIo",
                Genres = "Sci-Fi, Action",
                AgeRating = 16,
                DurationMinutes = 148,
                ReleaseDate = new DateTime(2025, 12, 22)
            },
            new Movie
            {
                Title = "Dune: Part Three",
                Description = "The epic conclusion of the Dune saga as Paul Atreides faces his ultimate destiny.",
                PosterUrl = "https://posterspy.com/wp-content/uploads/2025/08/dune3_deviant_diamonddead1.jpg",
                Actors = "Timothée Chalamet, Zendaya, Florence Pugh",
                TrailerUrl = "https://www.youtube.com/watch?v=3_9vCamtuPY",
                Genres = "Sci-Fi, Adventure",
                AgeRating = 12,
                DurationMinutes = 155,
                ReleaseDate = new DateTime(2026, 1, 15)
            },
            new Movie
            {
                Title = "Spider-Man: Beyond",
                Description = "Spider-Man faces threats from across the multiverse in this action-packed adventure.",
                PosterUrl = "https://s3.us-east-2.amazonaws.com/media.trendsinternational.com/21042-basecapture.jpg",
                Actors = "Tom Holland, Zendaya, Jacob Batalon",
                TrailerUrl = "https://www.youtube.com/watch?v=s0vxJjxbLUQ",
                Genres = "Action, Superhero",
                AgeRating = 12,
                DurationMinutes = 130,
                ReleaseDate = new DateTime(2026, 2, 1)
            },
            new Movie
            {
                Title = "Inside Out 3",
                Description = "Riley's emotions embark on yet another adventure as she navigates adulthood.",
                PosterUrl = "https://images-wixmp-ed30a86b8c4ca887773594c2.wixmp.com/f/1a5a7d4b-1fdc-4509-85b6-15ce08e91e60/di0576w-39b5b634-f9ee-4ede-a57e-31187b4bff2b.png?token=eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ1cm46YXBwOjdlMGQxODg5ODIyNjQzNzNhNWYwZDQxNWVhMGQyNmUwIiwiaXNzIjoidXJuOmFwcDo3ZTBkMTg4OTgyMjY0MzczYTVmMGQ0MTVlYTBkMjZlMCIsIm9iaiI6W1t7InBhdGgiOiIvZi8xYTVhN2Q0Yi0xZmRjLTQ1MDktODViNi0xNWNlMDhlOTFlNjAvZGkwNTc2dy0zOWI1YjYzNC1mOWVlLTRlZGUtYTU3ZS0zMTE4N2I0YmZmMmIucG5nIn1dXSwiYXVkIjpbInVybjpzZXJ2aWNlOmZpbGUuZG93bmxvYWQiXX0.mSuyswYd5d6p2ZmvEaRx7yfMwcxTCUpIcxc3KcGmEhA",
                Actors = "Amy Poehler, Phyllis Smith, Bill Hader",
                TrailerUrl = "https://www.youtube.com/watch?v=3NOpJPGv9Zg",
                Genres = "Animation, Family",
                AgeRating = 6,
                DurationMinutes = 105,
                ReleaseDate = new DateTime(2026, 2, 14)
            },
            new Movie
            {
                Title = "The Batman: Gotham Nights",
                Description = "The Dark Knight returns to protect Gotham from a new wave of crime.",
                PosterUrl = "https://m.media-amazon.com/images/I/81zkE4hKUjL.jpg",
                Actors = "Robert Pattinson, Zoë Kravitz, Jeffrey Wright",
                TrailerUrl = "https://www.youtube.com/watch?v=wIXJAqs9dG4",
                Genres = "Action, Crime",
                AgeRating = 16,
                DurationMinutes = 152,
                ReleaseDate = new DateTime(2025, 11, 20)
            },
            new Movie
            {
                Title = "Frozen III",
                Description = "Elsa and Anna discover new magical realms beyond Arendelle.",
                PosterUrl = "https://cdn.kinocheck.com/i/k9qlh8vsz9.jpg",
                Actors = "Idina Menzel, Kristen Bell, Josh Gad",
                TrailerUrl = "https://www.youtube.com/watch?v=i_oE6WoOlLI",
                Genres = "Animation, Musical",
                AgeRating = 6,
                DurationMinutes = 110,
                ReleaseDate = new DateTime(2025, 12, 6)
            },
        ];
    }

    private static List<Showtime> CreateShowtimes(List<Movie> movies, List<Room> rooms)
    {
        var showtimes = new List<Showtime>();

        // Calculate the Monday of the current week
        var today = DateTime.Today;
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7; // Monday = 0
        var currentMonday = today.AddDays(-daysSinceMonday);

        // Seed current week (Mon-Sun)
        AddWeekShowtimes(showtimes, movies, rooms, currentMonday);

        // If today is Thursday (3) or later in the week, also seed next week
        if (daysSinceMonday >= 3) // Thursday = 3 (Mon=0, Tue=1, Wed=2, Thu=3)
        {
            var nextMonday = currentMonday.AddDays(7);
            AddWeekShowtimes(showtimes, movies, rooms, nextMonday);
        }

        return showtimes;
    }

    private static void AddWeekShowtimes(
        List<Showtime> showtimes,
        List<Movie> movies,
        List<Room> rooms,
        DateTime monday)
    {
        // Time slots for showtimes
        var timeSlots = new[] { 14, 17, 20 }; // 14:00, 17:00, 20:00

        // For each day of the week (Mon=0 through Sun=6)
        for (int day = 0; day < 7; day++)
        {
            var date = monday.AddDays(day);

            // Spread movies across rooms and time slots
            int roomOffset = 0;
            foreach (var room in rooms)
            {
                for (int slotIndex = 0; slotIndex < timeSlots.Length; slotIndex++)
                {
                    var movie = movies[(roomOffset + slotIndex) % movies.Count];
                    showtimes.Add(new Showtime
                    {
                        Movie = movie,
                        Room = room,
                        StartTime = date.AddHours(timeSlots[slotIndex]),
                    });
                }
                roomOffset++;
            }
        }
    }

    private static List<PinCard> CreatePinCards()
    {
        var random = new Random(42);
        var pinCards = new List<PinCard>();

        for (int i = 0; i < 20; i++)
        {
            pinCards.Add(new PinCard
            {
                PinCode = random.Next(0, 10000).ToString("D4")
            });
        }

        return pinCards;
    }
    
    private static List<Seat> CreateSeats(List<Row> rows)
    {
        var seats = new List<Seat>();

        foreach (var row in rows)
        {
            var seatCount = row.SeatCount;
            for (int i = 1; i <= seatCount; i++)
            {
                var seat = new Seat
                {
                    Room = row.Room,
                    Row = row.RowNumber,
                    SeatNumber = i
                };
                
                seats.Add(seat);
            }
        }

        return seats;
    }
    
    private static List<ShowtimeSeat> CreateShowtimeSeats(List<Showtime> showtimes, List<Seat> seats)
    {
        var showtimeSeats = new List<ShowtimeSeat>();

        foreach (var showtime in showtimes)
        {
            foreach (var seat in seats)
            {
                if (seat.Room.Number != showtime.Room.Number)
                    continue;

                var showtimeSeat = new ShowtimeSeat
                {
                    Seat = seat,
                    Showtime = showtime
                };
                
                showtimeSeats.Add(showtimeSeat);
            }
        }

        return showtimeSeats;
    }
}