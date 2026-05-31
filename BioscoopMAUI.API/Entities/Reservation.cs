namespace BioscoopMAUI.API.Entities;

public class Reservation
{
    public int Id { get; set; }
    public int ShowtimeId { get; set; }
    public string Auth0UserId { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }

    // Navigation properties
    public Showtime Showtime { get; set; }
    public ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();
    public ICollection<PopcornOrder> PopcornOrders { get; set; } = new List<PopcornOrder>();
}