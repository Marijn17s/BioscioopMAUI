using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class MoviesPage : ContentPage
{
    private readonly MoviesPageViewModel _viewModel;

    public MoviesPage(MoviesPageViewModel viewModel)
    {
        _viewModel = viewModel;

        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.HasMovies && _viewModel.LoadMoviesCommand.CanExecute(null))
        {
            _viewModel.LoadMoviesCommand.Execute(null);
        }
    }
}