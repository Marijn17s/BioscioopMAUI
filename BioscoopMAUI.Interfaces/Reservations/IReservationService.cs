using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Reservations;

public interface IReservationService
{
    Task<IEnumerable<ReservationResponseDto>> GetReservationsAsync();
    Task<ReservationResponseDto?> GetReservationAsync(int reservationId);
    Task ChangeSeatsAsync(int reservationId, IEnumerable<int> seatIds);
    Task CancelReservationAsync(int reservationId);
}