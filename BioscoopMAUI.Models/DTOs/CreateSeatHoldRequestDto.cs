namespace BioscoopMAUI.Models.DTOs;

public class CreateSeatHoldRequestDto
{
    public List<int> SeatIds { get; set; } = [];
}

public class CreateSeatHoldResponseDto
{
    public Guid HoldId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public PriceQuoteDto? PriceQuote { get; set; }
}

public class SeatHoldDetailsDto
{
    public Guid HoldId { get; set; }
    public int ShowtimeId { get; set; }
    public List<int> SeatIds { get; set; } = new();
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsExpired { get; set; }
    public List<PopcornOrderDto>? PopcornOrders { get; set; }
}