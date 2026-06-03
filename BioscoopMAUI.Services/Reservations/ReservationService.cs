using System.Net.Http.Json;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Reservations;

public class ReservationService(IHttpClientFactory httpClientFactory) : IReservationService
{
    public async Task<IEnumerable<ReservationResponseDto>> GetReservationsAsync()
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.GetAsync("api/reservations");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ReservationResponseDto>>();
        return result ?? [];
    }
}