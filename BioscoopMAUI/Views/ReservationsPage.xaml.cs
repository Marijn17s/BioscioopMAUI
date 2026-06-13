using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class ReservationsPage : ContentPage
{
    private readonly ReservationsPageViewModel _viewModel;

    public ReservationsPage(ReservationsPageViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.LoadReservationsCommand.CanExecute(null))
            _viewModel.LoadReservationsCommand.Execute(null);
    }
}