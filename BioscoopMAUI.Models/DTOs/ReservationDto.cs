namespace BioscoopMAUI.Models.DTOs;

public static class ReservationStatus
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

public record ReservationResponseDto(
    int Id,
    ShowtimeResponseDto Showtime,
    List<SeatDto> Seats,
    string MovieTitle,
    string RoomName,
    decimal TotalPrice,
    string Status,
    DateTime CreatedAt);

public record ReservationSeatChangeRequestDto(
    List<int> SeatIds);