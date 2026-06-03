using BioscoopMAUI.Navigation;
using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class ShowtimeDetailsPage : ContentPage, IQueryAttributable
{
    private readonly ShowtimeDetailsPageViewModel _viewModel;

    public ShowtimeDetailsPage(ShowtimeDetailsPageViewModel viewModel)
    {
        _viewModel = viewModel;

        BindingContext = viewModel;
        InitializeComponent();
    }

    public void ApplyNavigationAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(NavigationRoutes.ShowtimeIdParameter, out var showtimeIdValue) ||
            !query.TryGetValue(NavigationRoutes.MovieIdParameter, out var movieIdValue))
        {
            return;
        }

        if (!TryParseId(showtimeIdValue, out var showtimeId) || !TryParseId(movieIdValue, out var movieId))
            return;

        _ = _viewModel.InitializeAsync(showtimeId, movieId);
    }

    private static bool TryParseId(object value, out int id)
    {
        if (value is int intValue)
        {
            id = intValue;
            return true;
        }

        if (value is string text && int.TryParse(text, out var parsedId))
        {
            id = parsedId;
            return true;
        }

        id = 0;
        return false;
    }
}