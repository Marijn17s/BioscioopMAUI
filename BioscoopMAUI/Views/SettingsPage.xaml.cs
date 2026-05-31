using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsPageViewModel _viewModel;

    public SettingsPage(SettingsPageViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}