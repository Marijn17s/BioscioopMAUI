using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class HomePage : ContentPage
{
    private readonly HomePageViewModel _viewModel;

    public HomePage(HomePageViewModel viewModel)
    {
        _viewModel = viewModel;

        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.HasLoadedOnce && _viewModel.LoadHomeCommand.CanExecute(null))
            _viewModel.LoadHomeCommand.Execute(null);
    }
}