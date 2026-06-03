using System.Net.Http.Json;
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

    public async Task<IEnumerable<MovieResponseDto>> GetFavoriteMoviesAsync()
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.GetAsync("api/movies/favorites");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<MovieResponseDto>>();
        return result ?? [];
    }

    public async Task<IEnumerable<MovieResponseDto>> GetRecommendedMoviesAsync()
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.GetAsync("api/movies/recommendations");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<MovieResponseDto>>();
        return result ?? [];
    }

    public async Task<bool> SetFavoriteStatusAsync(int movieId, bool isFavorite)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.PutAsJsonAsync($"api/movies/{movieId}/favorite", new SetFavoriteMovieRequestDto(isFavorite));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FavoriteMovieStatusDto>();
        return result?.IsFavorite ?? isFavorite;
    }
}