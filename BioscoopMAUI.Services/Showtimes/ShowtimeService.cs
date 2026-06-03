using System.Net.Http.Json;
using BioscoopMAUI.Interfaces.Showtimes;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Showtimes;

public class ShowtimeService(IHttpClientFactory httpClientFactory) : IShowtimeService
{
    public async Task<IEnumerable<FilmsOverviewDto>> GetUpcomingShowtimesAsync()
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var result = await client.GetFromJsonAsync<IEnumerable<FilmsOverviewDto>>("api/films-overview");
        return result ?? [];
    }
}