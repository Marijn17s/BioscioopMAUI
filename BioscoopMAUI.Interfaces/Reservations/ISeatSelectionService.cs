using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Reservations;

public interface ISeatSelectionService
{
    Task<IEnumerable<SeatInfoDto>> GetSeatMapAsync(int showtimeId);
    Task<SeatSelectionResponseDto?> SuggestSeatsAsync(int showtimeId, int seatCount);
    Task<PriceQuoteDto?> GetPriceQuoteAsync(int showtimeId, int seatCount);
    Task<CreateSeatHoldResponseDto> CreateHoldAsync(int showtimeId, IEnumerable<int> seatIds);
    Task ReleaseHoldAsync(Guid holdId);
}