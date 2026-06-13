using BioscoopMAUI.Navigation;
using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class ReservationDetailsPage : ContentPage, IQueryAttributable
{
    private readonly ReservationDetailsPageViewModel _viewModel;

    public ReservationDetailsPage(ReservationDetailsPageViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationRoutes.ReservationIdParameter, out var reservationIdValue))
            return;

        if (reservationIdValue is int reservationId)
        {
            _ = _viewModel.InitializeAsync(reservationId);
            return;
        }

        if (reservationIdValue is string reservationIdText && int.TryParse(reservationIdText, out var parsedReservationId))
            _ = _viewModel.InitializeAsync(parsedReservationId);
    }

    private async void OnCancelReservationClicked(object? sender, EventArgs e)
    {
        var shouldCancel = await DisplayAlertAsync(
            "Cancel reservation?",
            "Your seats will be released and the payment will be refunded when possible.",
            "Cancel reservation",
            "Keep reservation");

        if (!shouldCancel)
            return;

        await _viewModel.CancelReservationAsync();
    }
}