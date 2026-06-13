namespace BioscoopMAUI.Models.DTOs;
public record SeatInfoDto(
    int Id,
    int Row,
    int SeatNumber,
    bool IsAvailable,
    int QualityScore
);

public record SeatSelectionRequestDto(
    int GroupSize
);
public record SeatSelectionResponseDto(
    List<SeatInfoDto> SuggestedSeats,
    string Message,
    bool IsGroupedTogether,
    List<List<SeatInfoDto>> GroupedSeats,
    double TotalScore
);

public record ReservationConfirmRequestDto(
    List<int> SeatIds,
    List<PopcornOrderDto>? PopcornOrders = null,
    decimal TotalPrice = 0m
);

public record ReservationConfirmResponseDto(
    int ReservationId,
    List<SeatInfoDto> ReservedSeats,
    string Message
);

public record PriceQuoteDto(
    int ShowtimeId,
    int SeatCount,
    decimal BaseTicketPrice,
    decimal SurchargePerTicket,
    decimal TicketPriceBeforeDiscount,
    decimal Subtotal,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    decimal TotalPrice
);

public record CheckoutSessionRequestDto(
    Guid HoldId
);

public record CheckoutSessionResponseDto(
    string SessionId,
    string CheckoutUrl,
    DateTime HoldExpiresAtUtc
);

public record PaymentStatusResponseDto(
    string Status,
    int? ReservationId,
    string? ErrorMessage
);