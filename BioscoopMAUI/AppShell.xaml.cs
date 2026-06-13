using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Navigation;
using BioscoopMAUI.Views;

namespace BioscoopMAUI;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;
    private bool _isRedirectingToLogin;

    public AppShell(IAuthService authService)
    {
        _authService = authService;
        
        InitializeComponent();
        
        RegisterRoutes();
        Loaded += OnShellLoadedAsync;
        Navigated += OnShellNavigatedAsync;
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(NavigationRoutes.MovieDetails, typeof(MovieDetailsPage));
        Routing.RegisterRoute(NavigationRoutes.ShowtimeDetails, typeof(ShowtimeDetailsPage));
        Routing.RegisterRoute(NavigationRoutes.ReservationDetails, typeof(ReservationDetailsPage));
        Routing.RegisterRoute(NavigationRoutes.SeatSelection, typeof(SeatSelectionPage));
    }

    private async void OnShellLoadedAsync(object? sender, EventArgs e)
    {
        Loaded -= OnShellLoadedAsync;
        await EnsureAuthenticatedNavigationAsync();
    }

    private async void OnShellNavigatedAsync(object? sender, ShellNavigatedEventArgs e)
    {
        await EnsureAuthenticatedNavigationAsync();
    }

    private async Task EnsureAuthenticatedNavigationAsync()
    {
        if (_isRedirectingToLogin)
            return;

        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        var isOnLoginRoute = CurrentState.Location.OriginalString.Contains(NavigationRoutes.Login);

        if (!isAuthenticated)
        {
            SetTabBarIsVisible(this, false);

            if (isOnLoginRoute) return;
            
            _isRedirectingToLogin = true;
            try
            {
                await GoToAsync($"//{NavigationRoutes.Login}");
            }
            finally
            {
                _isRedirectingToLogin = false;
            }
            return;
        }

        SetTabBarIsVisible(this, true);

        if (isOnLoginRoute)
            await GoToAsync($"//{NavigationRoutes.Home}");
    }
}