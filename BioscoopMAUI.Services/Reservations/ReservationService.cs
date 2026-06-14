using System.Net.Http.Json;
using BioscoopMAUI.Interfaces.Notifications;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Reservations;

public class ReservationService(IHttpClientFactory httpClientFactory, ILocalReservationStore localReservationStore, INotificationService notificationService) : IReservationService
{
    public async Task<IEnumerable<ReservationResponseDto>> GetReservationsAsync()
    {
        List<ReservationResponseDto> reservations;
        try
        {
            var client = httpClientFactory.CreateClient("BioscoopAPI");
            var response = await client.GetAsync("api/reservations");
            response.EnsureSuccessStatusCode();
            reservations = (await response.Content.ReadFromJsonAsync<IEnumerable<ReservationResponseDto>>())?.ToList() ?? [];
            await localReservationStore.SaveReservationsAsync(reservations);
        }
        catch (HttpRequestException)
        {
            reservations = await localReservationStore.GetReservationsAsync();
        }

        await notificationService.SyncRemindersAsync(reservations);
        return reservations;
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

    public async Task<QrCodeValidationResponseDto> ValidateQrCodeAsync(string qrCode)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var response = await client.PostAsJsonAsync(
            "api/reservations/validate-qr",
            new QrCodeValidationRequestDto(qrCode),
            cancellationTokenSource.Token);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<QrCodeValidationResponseDto>(cancellationTokenSource.Token)
            ?? new QrCodeValidationResponseDto(false, null, "We couldn't validate this ticket.");
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