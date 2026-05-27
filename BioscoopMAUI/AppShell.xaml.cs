using BioscoopMAUI.Navigation;
using BioscoopMAUI.Views;

namespace BioscoopMAUI;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(NavigationRoutes.MovieDetails, typeof(MovieDetailsPage));
    }
}