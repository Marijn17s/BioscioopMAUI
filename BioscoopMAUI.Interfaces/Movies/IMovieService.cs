using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Movies;

public interface IMovieService
{
    Task<IEnumerable<MovieResponseDto>> GetAllMoviesAsync();
    Task<MovieResponseDto?> GetMovieByIdAsync(int id);
}