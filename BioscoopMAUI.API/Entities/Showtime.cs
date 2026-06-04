namespace BioscoopMAUI.API.Entities;

public class Showtime
{
    public int Id { get; set; }

    public int MovieId { get; set; }

    public int RoomId { get; set; }

    public DateTime StartTime { get; set; }

    public decimal DiscountPercentage { get; set; }

    // Navigation properties
    public Movie Movie { get; set; }
    public Room Room { get; set; }
    public ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();
}