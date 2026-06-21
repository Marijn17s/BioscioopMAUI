using BioscoopMAUI.API.Entities;
using BioscoopMAUI.API.Services;
using NUnit.Framework;

namespace BioscoopMAUI.API.Tests.Services;

[TestFixture]
public class SeatSelectionServiceTests
{
    [Test]
    public void CalculateSeatScore_FavorsCenterSeatsOverEdgeSeats()
    {
        const int totalRows = 10;
        const int seatsPerRow = 10;

        var centerScore = SeatSelectionService.CalculateSeatScore(5, 5, totalRows, seatsPerRow);
        var edgeScore = SeatSelectionService.CalculateSeatScore(1, 1, totalRows, seatsPerRow);

        Assert.That(centerScore, Is.LessThan(edgeScore));
    }

    [Test]
    public void SelectBestSeats_ReturnsContiguousBlockWhenAvailable()
    {
        var showtime = CreateShowtimeWithSeats(
            rows: 10,
            seatsPerRow: 10,
            occupiedSeatNumbers: [1, 2, 3, 4, 8, 9, 10]);

        var result = SeatSelectionService.SelectBestSeats(showtime, groupSize: 3, showtime.ShowtimeSeats.AsQueryable());

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedSeats, Has.Count.EqualTo(3));
            Assert.That(result.IsGroupedTogether, Is.True);
            Assert.That(result.SelectedSeats.Select(seat => seat.SeatNumber).OrderBy(number => number), Is.EqualTo(new[] { 5, 6, 7 }));
        });
    }

    [Test]
    public void SelectBestSeats_SplitsGroupWhenNoContiguousBlockFits()
    {
        var showtime = CreateShowtimeWithSeats(
            rows: 10,
            seatsPerRow: 10,
            occupiedSeatNumbers: [3, 4, 5, 6, 7, 8]);

        var result = SeatSelectionService.SelectBestSeats(showtime, groupSize: 4, showtime.ShowtimeSeats.AsQueryable());

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedSeats, Has.Count.EqualTo(4));
            Assert.That(result.IsGroupedTogether, Is.False);
            Assert.That(result.GroupedSeats, Has.Count.EqualTo(2));
            Assert.That(result.GroupedSeats.Select(group => group.Count), Is.EquivalentTo(new[] { 2, 2 }));
        });
    }

    private static Showtime CreateShowtimeWithSeats(int rows, int seatsPerRow, IEnumerable<int> occupiedSeatNumbers)
    {
        var room = new Room
        {
            Id = 1,
            Number = 1,
            Name = "Room 1",
            Has3D = false,
            IsWheelchairAccessible = true
        };

        var seatId = 1;
        for (var row = 1; row <= rows; row++)
        {
            room.Rows.Add(new Row
            {
                Id = row,
                RoomId = room.Id,
                RowNumber = row,
                SeatCount = seatsPerRow
            });

            for (var seatNumber = 1; seatNumber <= seatsPerRow; seatNumber++)
            {
                room.Seats.Add(new Seat
                {
                    Id = seatId,
                    RoomId = room.Id,
                    Row = row,
                    SeatNumber = seatNumber
                });
                seatId++;
            }
        }

        var showtime = new Showtime
        {
            Id = 1,
            MovieId = 1,
            RoomId = room.Id,
            StartTime = DateTime.UtcNow.AddHours(2),
            Room = room,
            Movie = new Movie
            {
                Id = 1,
                Title = "Test Movie",
                Description = "Description",
                PosterUrl = "poster.jpg",
                Actors = "Actor",
                TrailerUrl = "trailer.mp4",
                Genres = "Action",
                AgeRating = 12,
                DurationMinutes = 100,
                ReleaseDate = DateTime.UtcNow
            }
        };

        var occupiedSet = occupiedSeatNumbers.ToHashSet();
        foreach (var seat in room.Seats.Where(seat => occupiedSet.Contains(seat.SeatNumber)))
        {
            showtime.ShowtimeSeats.Add(new ShowtimeSeat
            {
                ShowtimeId = showtime.Id,
                SeatId = seat.Id,
                ReservationId = 99,
                Seat = seat,
                Showtime = showtime
            });
        }

        return showtime;
    }
}