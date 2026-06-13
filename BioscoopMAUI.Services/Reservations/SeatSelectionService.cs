using System.Net.Http.Json;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Reservations;

public class SeatSelectionService(IHttpClientFactory httpClientFactory) : ISeatSelectionService
{
    public async Task<IEnumerable<SeatInfoDto>> GetSeatMapAsync(int showtimeId)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var result = await client.GetFromJsonAsync<IEnumerable<SeatInfoDto>>($"api/seat-selection/{showtimeId}/available");
        return result ?? [];
    }

    public async Task<SeatSelectionResponseDto?> SuggestSeatsAsync(int showtimeId, int seatCount)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.PostAsJsonAsync($"api/seat-selection/{showtimeId}/suggest", new SeatSelectionRequestDto(seatCount));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SeatSelectionResponseDto>();
    }

    public async Task<PriceQuoteDto?> GetPriceQuoteAsync(int showtimeId, int seatCount)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        return await client.GetFromJsonAsync<PriceQuoteDto>($"api/seat-selection/{showtimeId}/price-quote?seatCount={seatCount}");
    }

    public async Task<CreateSeatHoldResponseDto> CreateHoldAsync(int showtimeId, IEnumerable<int> seatIds)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.PostAsJsonAsync($"api/seat-selection/{showtimeId}/holds", new CreateSeatHoldRequestDto
        {
            SeatIds = seatIds.ToList()
        });

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateSeatHoldResponseDto>()
            ?? throw new InvalidOperationException("The seat hold response was empty.");
    }

    public async Task ReleaseHoldAsync(Guid holdId)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.DeleteAsync($"api/seat-selection/holds/{holdId}");
        response.EnsureSuccessStatusCode();
    }
}