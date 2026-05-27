using BioscoopMAUI.Navigation;
using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class MovieDetailsPage : ContentPage, IQueryAttributable
{
    private readonly MovieDetailsPageViewModel _viewModel;

    public MovieDetailsPage(MovieDetailsPageViewModel viewModel)
    {
        _viewModel = viewModel;

        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationRoutes.MovieIdParameter, out var movieIdValue))
        {
            return;
        }

        if (movieIdValue is int movieId)
        {
            _ = _viewModel.InitializeAsync(movieId);
            return;
        }

        if (movieIdValue is string movieIdText && int.TryParse(movieIdText, out var parsedMovieId))
        {
            _ = _viewModel.InitializeAsync(parsedMovieId);
        }
    }
}
