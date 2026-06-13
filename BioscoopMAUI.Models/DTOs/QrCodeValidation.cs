namespace BioscoopMAUI.Models.DTOs;

public record QrCodeValidationRequestDto(string QrCode);

public record QrCodeValidationResponseDto(
    bool IsValid,
    int? ReservationId,
    string? ErrorMessage,
    string? MovieTitle = null,
    string? RoomName = null,
    DateTime? ShowtimeStartTime = null,
    List<SeatDto>? Seats = null);