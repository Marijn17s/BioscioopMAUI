namespace BioscoopMAUI.API.Entities;

public class ShowtimeSeat
{
    public int ShowtimeId { get; set; }

    public int SeatId { get; set; }

    public int? ReservationId { get; set; }

    public Guid? HoldId { get; set; }

    public string? HoldAuth0UserId { get; set; }

    public DateTime? HoldExpiresAtUtc { get; set; }

    // Navigation properties
    public Showtime Showtime { get; set; }
    public Seat Seat { get; set; }
    public Reservation Reservation { get; set; }
}
