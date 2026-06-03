using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Reservations;

public interface IReservationService
{
    Task<IEnumerable<ReservationResponseDto>> GetReservationsAsync();
}