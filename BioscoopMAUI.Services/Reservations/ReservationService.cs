using System.Net.Http.Json;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Reservations;

public class ReservationService(IHttpClientFactory httpClientFactory, ILocalReservationStore localReservationStore) : IReservationService
{
    public async Task<IEnumerable<ReservationResponseDto>> GetReservationsAsync()
    {
        try
        {
            var client = httpClientFactory.CreateClient("BioscoopAPI");
            var response = await client.GetAsync("api/reservations");
            response.EnsureSuccessStatusCode();
            var result = (await response.Content.ReadFromJsonAsync<IEnumerable<ReservationResponseDto>>())?.ToList() ?? [];
            await localReservationStore.SaveReservationsAsync(result);
            return result;
        }
        catch (HttpRequestException)
        {
            return await localReservationStore.GetReservationsAsync();
        }
    }

    public async Task<ReservationResponseDto?> GetReservationAsync(int reservationId)
    {
        try
        {
            var client = httpClientFactory.CreateClient("BioscoopAPI");
            var response = await client.GetAsync($"api/reservations/{reservationId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            var reservation = await response.Content.ReadFromJsonAsync<ReservationResponseDto>();
            if (reservation is not null)
                await localReservationStore.SaveReservationAsync(reservation);

            return reservation;
        }
        catch (HttpRequestException)
        {
            return (await localReservationStore.GetReservationsAsync()).FirstOrDefault(reservation => reservation.Id == reservationId);
        }
    }

    public async Task ChangeSeatsAsync(int reservationId, IEnumerable<int> seatIds)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.PutAsJsonAsync($"api/reservations/{reservationId}/seats", new ReservationSeatChangeRequestDto(seatIds.ToList()));
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelReservationAsync(int reservationId)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.DeleteAsync($"api/reservations/{reservationId}");
        response.EnsureSuccessStatusCode();
        await localReservationStore.RemoveReservationAsync(reservationId);
    }
}