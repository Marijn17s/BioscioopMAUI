using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Models.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class MovieDetailsPageViewModel(IMovieService movieService) : ObservableObject
{
    private static readonly TimeSpan ShowtimeLookAhead = TimeSpan.FromDays(7);
    private static readonly TimeSpan FavoriteErrorDisplayDuration = TimeSpan.FromSeconds(5);
    
    private CancellationTokenSource? _favoriteErrorDismissCancellation;

    public ObservableCollection<ShowtimeResponseDto> Showtimes { get; } = [];

    public ObservableCollection<string> Genres { get; } = [];

    public bool HasMovie => Movie is not null;

    public bool HasGenres => Genres.Count > 0;

    public bool HasShowtimes => Showtimes.Count > 0;

    public bool HasNoShowtimes => !HasShowtimes;

    public bool HasLoadError => !string.IsNullOrWhiteSpace(LoadErrorMessage);

    public bool HasFavoriteError => !string.IsNullOrWhiteSpace(FavoriteErrorMessage);

    public bool HasTrailer => !string.IsNullOrWhiteSpace(Movie?.TrailerUrl);

    public bool CanToggleFavorite => !IsBusy && !IsTogglingFavorite;

    public string FavoriteIcon => IsFavorite
        ? Constants.TabIconGlyphs.Favorite
        : Constants.TabIconGlyphs.FavoriteBorder;

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

        IsFavorite = value?.IsFavorite ?? false;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteIcon))]
    [NotifyPropertyChangedFor(nameof(CanToggleFavorite))]
    private bool _isFavorite;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggleFavorite))]
    private bool _isTogglingFavorite;

    [RelayCommand(CanExecute = nameof(HasTrailer))]
    private async Task OpenTrailerAsync()
    {
        if (Movie is null || string.IsNullOrWhiteSpace(Movie.TrailerUrl))
            return;

        await Launcher.Default.OpenAsync(new Uri(Movie.TrailerUrl));
    }

    [RelayCommand(CanExecute = nameof(CanToggleFavorite))]
    private async Task ToggleFavoriteAsync()
    {
        if (!CanToggleFavorite || Movie is null)
            return;

        IsTogglingFavorite = true;
        FavoriteErrorMessage = string.Empty;

        try
        {
            IsFavorite = await movieService.SetFavoriteStatusAsync(Movie.Id, !IsFavorite);
        }
        catch (Exception)
        {
            FavoriteErrorMessage = "We could not update your favourite. Please try again.";
        }
        finally
        {
            IsTogglingFavorite = false;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanToggleFavorite))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLoadError))]
    private string _loadErrorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFavoriteError))]
    private string _favoriteErrorMessage = string.Empty;

    partial void OnFavoriteErrorMessageChanged(string value)
    {
        CancelFavoriteErrorDismiss();

        if (string.IsNullOrWhiteSpace(value))
            return;

        _favoriteErrorDismissCancellation = new CancellationTokenSource();
        _ = DismissFavoriteErrorAfterDelayAsync(_favoriteErrorDismissCancellation.Token);
    }

    public async Task InitializeAsync(int movieId)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        LoadErrorMessage = string.Empty;
        FavoriteErrorMessage = string.Empty;

        try
        {
            Movie = await movieService.GetMovieByIdAsync(movieId);
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
            LoadErrorMessage = "We kunnen deze film nu niet laden. Probeer het later opnieuw.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DismissFavoriteErrorAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(FavoriteErrorDisplayDuration, cancellationToken);
            await MainThread.InvokeOnMainThreadAsync(() => FavoriteErrorMessage = string.Empty);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelFavoriteErrorDismiss()
    {
        _favoriteErrorDismissCancellation?.Cancel();
        _favoriteErrorDismissCancellation?.Dispose();
        _favoriteErrorDismissCancellation = null;
    }
}