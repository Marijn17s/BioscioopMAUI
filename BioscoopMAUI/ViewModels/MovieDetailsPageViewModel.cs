using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Models.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class MovieDetailsPageViewModel : ObservableObject
{
    private readonly IMovieService _movieService;
    private static readonly TimeSpan ShowtimeLookAhead = TimeSpan.FromDays(7);

    public MovieDetailsPageViewModel(IMovieService movieService)
    {
        _movieService = movieService;
    }

    public ObservableCollection<ShowtimeResponseDto> Showtimes { get; } = [];

    public ObservableCollection<string> Genres { get; } = [];

    public bool HasMovie => Movie is not null;

    public bool HasGenres => Genres.Count > 0;

    public bool HasShowtimes => Showtimes.Count > 0;

    public bool HasNoShowtimes => !HasShowtimes;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasTrailer => !string.IsNullOrWhiteSpace(Movie?.TrailerUrl);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMovie))]
    [NotifyPropertyChangedFor(nameof(HasGenres))]
    [NotifyPropertyChangedFor(nameof(HasTrailer))]
    private MovieResponseDto? _movie;

    partial void OnMovieChanged(MovieResponseDto? value)
    {
        Genres.Clear();

        if (!string.IsNullOrWhiteSpace(value?.Genres))
            foreach (var genre in value.Genres.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                Genres.Add(genre);
    }

    [RelayCommand(CanExecute = nameof(HasTrailer))]
    private async Task OpenTrailerAsync()
    {
        if (Movie is null || string.IsNullOrWhiteSpace(Movie.TrailerUrl))
            return;

        await Launcher.Default.OpenAsync(new Uri(Movie.TrailerUrl));
    }

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public async Task InitializeAsync(int movieId)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            Movie = await _movieService.GetMovieByIdAsync(movieId);
            Showtimes.Clear();

            var now = DateTime.Now;
            var maximumShowtimeDate = now.Add(ShowtimeLookAhead);
            var upcomingShowtimes = (Movie?.Showtimes ?? [])
                .Where(showtime => showtime.StartTime >= now && showtime.StartTime <= maximumShowtimeDate)
                .OrderBy(showtime => showtime.StartTime);

            foreach (var showtime in upcomingShowtimes)
            {
                Showtimes.Add(showtime);
            }

            OnPropertyChanged(nameof(HasShowtimes));
            OnPropertyChanged(nameof(HasNoShowtimes));
        }
        catch (Exception)
        {
            ErrorMessage = "We kunnen deze film nu niet laden. Probeer het later opnieuw.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
