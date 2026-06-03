using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class ShowtimesPage : ContentPage
{
    private readonly ShowtimesPageViewModel _viewModel;

    public ShowtimesPage(ShowtimesPageViewModel viewModel)
    {
        _viewModel = viewModel;

        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.HasLoadedOnce && _viewModel.LoadScreeningsCommand.CanExecute(null))
            _viewModel.LoadScreeningsCommand.Execute(null);
    }
}