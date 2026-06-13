using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Reservations;

public interface ITicketPdfService
{
    Task<string> GenerateReservationTicketsAsync(ReservationResponseDto reservation);
}