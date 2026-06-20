using BioscoopMAUI.ViewModels;
using BioscoopMAUI.ViewModels.UI;

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

        if (!_viewModel.HasLoadedOnce && _viewModel.LoadShowtimesCommand.CanExecute(null))
            _viewModel.LoadShowtimesCommand.Execute(null);
    }

    private void OnDateFilterCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is not RadioButton radioButton || !e.Value)
            return;

        if (radioButton.BindingContext is ShowtimeFilterOption option)
            _viewModel.SelectDateFilterCommand.Execute(option);
    }

    private void OnTimeFilterCheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        if (sender is not RadioButton radioButton || !e.Value)
            return;

        if (radioButton.BindingContext is ShowtimeFilterOption option)
            _viewModel.SelectTimeFilterCommand.Execute(option);
    }

    private void OnMovieSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.NewTextValue))
            return;

        _viewModel.ClearMovieSearchCommand.Execute(null);
    }

    private void OnMovieSearchButtonPressed(object? sender, EventArgs e)
    {
        if (sender is SearchBar searchBar)
            searchBar.Unfocus();
    }
}