using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Models.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BioscoopMAUI.ViewModels;

public partial class ShowtimeDetailsPageViewModel(IMovieService movieService) : ObservableObject
{
    public ObservableCollection<string> Genres { get; } = [];

    public bool HasMovie => Movie is not null;

    public bool HasShowtime => Showtime is not null;

    public bool HasGenres => GenreCount > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGenres))]
    private int _genreCount;

    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    public bool CanBookTickets => false; // TODO: implement

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMovie))]
    [NotifyPropertyChangedFor(nameof(HasGenres))]
    private MovieResponseDto? _movie;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasShowtime))]
    private ShowtimeResponseDto? _showtime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadError))]
    private string _loadErrorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public async Task InitializeAsync(int showtimeId, int movieId)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        LoadErrorMessage = string.Empty;
        Movie = null;
        Showtime = null;
        Genres.Clear();

        try
        {
            var movie = await movieService.GetMovieByIdAsync(movieId);
            if (movie is null)
            {
                LoadErrorMessage = "This showtime is not available. Please try again later.";
                return;
            }

            var showtime = movie.Showtimes?.FirstOrDefault(item => item.Id == showtimeId);
            if (showtime is null)
            {
                LoadErrorMessage = "This showtime is no longer available.";
                return;
            }

            Movie = movie;
            Showtime = showtime;

            if (!string.IsNullOrWhiteSpace(movie.Genres))
            {
                foreach (var genre in movie.Genres.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    Genres.Add(genre);
            }

            GenreCount = Genres.Count;
        }
        catch (Exception)
        {
            LoadErrorMessage = "This showtime is not available. Please try again later.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}