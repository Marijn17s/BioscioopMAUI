using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Movies;

public class MovieService(IHttpClientFactory httpClientFactory) : IMovieService
{
    public async Task<IEnumerable<MovieResponseDto>> GetAllMoviesAsync()
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var result = await client.GetFromJsonAsync<IEnumerable<MovieResponseDto>>("api/movies");
        return result ?? [];
    }

    public async Task<MovieResponseDto?> GetMovieByIdAsync(int id)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        return await client.GetFromJsonAsync<MovieResponseDto>($"api/movies/{id}");
    }
}