using BioscoopMAUI.API.Entities;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.API.Services;

public interface ITicketPricingService
{
    Task<PriceQuoteDto> GetPriceQuoteAsync(Showtime showtime, int seatCount);
}