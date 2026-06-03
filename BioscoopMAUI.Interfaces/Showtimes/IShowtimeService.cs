using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Showtimes;

public interface IShowtimeService
{
    Task<IEnumerable<FilmsOverviewDto>> GetUpcomingShowtimesAsync();
}