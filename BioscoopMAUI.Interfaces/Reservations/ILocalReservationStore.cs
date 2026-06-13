using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Reservations;

public interface ILocalReservationStore
{
    Task<List<ReservationResponseDto>> GetReservationsAsync();
    Task SaveReservationsAsync(IEnumerable<ReservationResponseDto> reservations);
    Task SaveReservationAsync(ReservationResponseDto reservation);
    Task RemoveReservationAsync(int reservationId);
}