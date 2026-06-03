using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class MoviesPageViewModel(IMovieService movieService, INavigationService navigationService) : ObservableObject
{
    public ObservableCollection<MovieResponseDto> Movies { get; } = [];

    public bool HasMovies => LoadedMovieCount > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmptyStateVisible => !IsBusy && !HasError && HasLoadedOnce && LoadedMovieCount is 0;

    public bool ShowMoviesList => HasLoadedOnce && !HasError && !IsEmptyStateVisible;

    public bool IsInitialLoading => IsBusy && !HasLoadedOnce;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(ShowMoviesList))]
    [NotifyPropertyChangedFor(nameof(IsInitialLoading))]
    private bool _hasLoadedOnce;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMovies))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(ShowMoviesList))]
    private int _loadedMovieCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(ShowMoviesList))]
    [NotifyPropertyChangedFor(nameof(IsInitialLoading))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(ShowMoviesList))]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoadMoviesAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var movies = await movieService.GetAllMoviesAsync();

            Movies.Clear();
            foreach (var movie in movies)
            {
                Movies.Add(movie);
            }

            HasLoadedOnce = true;
            LoadedMovieCount = Movies.Count;
        }
        catch (Exception)
        {
            if (!HasLoadedOnce)
                ErrorMessage = "We can't load the movies. Please check your connection and try again.";
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