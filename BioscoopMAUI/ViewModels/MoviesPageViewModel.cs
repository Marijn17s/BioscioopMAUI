using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class MoviesPageViewModel : ObservableObject
{
    private readonly IMovieService _movieService;
    private readonly INavigationService _navigationService;

    public MoviesPageViewModel(IMovieService movieService, INavigationService navigationService)
    {
        _movieService = movieService;
        _navigationService = navigationService;

        Movies.CollectionChanged += OnMoviesChanged;
    }

    public ObservableCollection<MovieResponseDto> Movies { get; } = [];

    public bool HasMovies => Movies.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmptyStateVisible => !IsBusy && !HasError && Movies.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoadMoviesAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var movies = await _movieService.GetAllMoviesAsync();

            Movies.Clear();
            foreach (var movie in movies)
            {
                Movies.Add(movie);
            }
        }
        catch (Exception ex)
        {
            var test = ex.Message;
            ErrorMessage = "We kunnen de films nu niet laden. Controleer je verbinding en probeer opnieuw.";
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
        {
            return;
        }
        
        await _navigationService.GoToAsync(NavigationRoutes.MovieDetails,
            new Dictionary<string, object>
            {
                [NavigationRoutes.MovieIdParameter] = movie.Id
            });
    }

    private void OnMoviesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasMovies));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
    }
}