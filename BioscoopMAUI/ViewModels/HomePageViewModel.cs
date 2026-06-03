using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class HomePageViewModel(
    IMovieService movieService,
    IReservationService reservationService,
    INavigationService navigationService)
    : ObservableObject
{
    public ObservableCollection<MovieResponseDto> RecommendedMovies { get; } = [];

    public ObservableCollection<MovieResponseDto> FavoriteMovies { get; } = [];

    public ObservableCollection<ReservationResponseDto> UpcomingReservations { get; } = [];

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsInitialLoading => IsBusy && !HasLoadedOnce;

    public bool ShowContent => HasLoadedOnce && !HasError;

    public bool ShowRecommendations => ShowContent && RecommendedCount > 0;

    public bool HasFavorites => FavoriteCount > 0;

    public bool ShowFavoritesEmpty => ShowContent && FavoriteCount is 0;

    public bool HasUpcomingReservations => UpcomingCount > 0;

    public bool ShowUpcomingEmpty => ShowContent && UpcomingCount is 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInitialLoading))]
    [NotifyPropertyChangedFor(nameof(ShowContent))]
    [NotifyPropertyChangedFor(nameof(ShowRecommendations))]
    [NotifyPropertyChangedFor(nameof(ShowFavoritesEmpty))]
    [NotifyPropertyChangedFor(nameof(ShowUpcomingEmpty))]
    private bool _hasLoadedOnce;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInitialLoading))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ShowContent))]
    [NotifyPropertyChangedFor(nameof(ShowRecommendations))]
    [NotifyPropertyChangedFor(nameof(ShowFavoritesEmpty))]
    [NotifyPropertyChangedFor(nameof(ShowUpcomingEmpty))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRecommendations))]
    private int _recommendedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFavorites))]
    [NotifyPropertyChangedFor(nameof(ShowFavoritesEmpty))]
    private int _favoriteCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpcomingReservations))]
    [NotifyPropertyChangedFor(nameof(ShowUpcomingEmpty))]
    private int _upcomingCount;

    [RelayCommand]
    private async Task LoadHomeAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var recommendedTask = movieService.GetRecommendedMoviesAsync();
            var favoritesTask = movieService.GetFavoriteMoviesAsync();
            var reservationsTask = reservationService.GetReservationsAsync();

            await Task.WhenAll(recommendedTask, favoritesTask, reservationsTask);

            RecommendedMovies.Clear();
            foreach (var movie in await recommendedTask)
            {
                RecommendedMovies.Add(movie);
            }

            FavoriteMovies.Clear();
            foreach (var movie in await favoritesTask)
            {
                FavoriteMovies.Add(movie);
            }

            var upcomingReservations = (await reservationsTask)
                .Where(reservation => reservation.Showtime.StartTime >= DateTime.Now)
                .OrderBy(reservation => reservation.Showtime.StartTime);

            UpcomingReservations.Clear();
            foreach (var reservation in upcomingReservations)
            {
                UpcomingReservations.Add(reservation);
            }

            RecommendedCount = RecommendedMovies.Count;
            FavoriteCount = FavoriteMovies.Count;
            UpcomingCount = UpcomingReservations.Count;

            HasLoadedOnce = true;
        }
        catch (Exception)
        {
            if (!HasLoadedOnce)
                ErrorMessage = "We can't load your personalized homepage. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenMovieDetailsAsync(MovieResponseDto? movie)
    {
        if (movie is null)
            return;

        await navigationService.GoToAsync(NavigationRoutes.MovieDetails,
            new Dictionary<string, object>
            {
                [NavigationRoutes.MovieIdParameter] = movie.Id
            });
    }
}