using BioscoopMAUI.Models.Constants;
using BioscoopMAUI.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace BioscoopMAUI.Views;

public partial class HomePage : ContentPage
{
    private readonly HomePageViewModel _viewModel;
    private bool _hasConfiguredMap;

    public HomePage(HomePageViewModel viewModel)
    {
        _viewModel = viewModel;

        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ConfigureCinemaMap();

        if (!_viewModel.HasLoadedOnce && _viewModel.LoadHomeCommand.CanExecute(null))
            _viewModel.LoadHomeCommand.Execute(null);
    }

    private void ConfigureCinemaMap()
    {
        if (_hasConfiguredMap)
            return;

        var cinemaLocation = new Location(CinemaLocation.Latitude, CinemaLocation.Longitude);

        CinemaMap.Pins.Add(new Pin
        {
            Label = CinemaLocation.Name,
            Address = $"{CinemaLocation.Street} {CinemaLocation.StreetNumber}, {CinemaLocation.City}",
            Location = cinemaLocation
        });

        CinemaMap.MoveToRegion(MapSpan.FromCenterAndRadius(cinemaLocation, Microsoft.Maui.Maps.Distance.FromKilometers(1)));

        _hasConfiguredMap = true;
    }
}