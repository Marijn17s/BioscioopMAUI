using BioscoopMAUI.Navigation;
using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class SeatSelectionPage : ContentPage, IQueryAttributable
{
    private readonly SeatSelectionPageViewModel _viewModel;

    public SeatSelectionPage(SeatSelectionPageViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var showtimeId = GetInt(query, NavigationRoutes.ShowtimeIdParameter);
        if (showtimeId is null)
            return;

        var movieTitle = GetString(query, NavigationRoutes.MovieTitleParameter);
        var roomName = GetString(query, NavigationRoutes.RoomNameParameter);
        var startTime = GetDateTime(query, NavigationRoutes.StartTimeParameter) ?? DateTime.Now;
        var reservationId = GetInt(query, NavigationRoutes.ReservationIdParameter);

        _ = _viewModel.InitializeAsync(showtimeId.Value, movieTitle, roomName, startTime, reservationId);
    }

    private async void OnTicketCountStepperValueChanged(object? sender, ValueChangedEventArgs e)
        => await _viewModel.OnTicketCountStepperChangedAsync((int)e.NewValue);

    private static string GetString(IDictionary<string, object> query, string key)
        => query.TryGetValue(key, out var value) ? value.ToString() ?? string.Empty : string.Empty;

    private static int? GetInt(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            int number => number,
            string text when int.TryParse(text, out var number) => number,
            _ => null
        };
    }

    private static DateTime? GetDateTime(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            DateTime dateTime => dateTime,
            string text when DateTime.TryParse(text, out var dateTime) => dateTime,
            _ => null
        };
    }
}