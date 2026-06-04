namespace BioscoopMAUI.Models.DTOs;

public record ShowtimeResponseDto(
    int Id,
    int MovieId,
    int RoomId,
    string RoomName,
    DateTime StartTime,
    double TicketPrice,
    decimal DiscountPercentage
);

public record ShowtimeCreateDto(
    int MovieId,
    int RoomId,
    DateTime StartTime,
    decimal DiscountPercentage = 0
);

public record ShowtimeBulkCreateDto(
    List<ShowtimeCreateDto> Showtimes
);
