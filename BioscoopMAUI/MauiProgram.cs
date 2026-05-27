using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Services.Movies;
using BioscoopMAUI.Services.Navigation;
using BioscoopMAUI.ViewModels;
using BioscoopMAUI.Views;
using Microsoft.Maui.Devices;
using Microsoft.Extensions.Logging;

namespace BioscoopMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIconsOutlined-Regular.otf", "MaterialIconsOutlined");
            });

        builder.Services.AddHttpClient("BioscoopAPI", client =>
        {
            client.BaseAddress = new Uri("http://192.168.2.27:5064/");
        });

        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IMovieService, MovieService>();

        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MoviesPageViewModel>();
        builder.Services.AddTransient<MovieDetailsPageViewModel>();

        builder.Services.AddTransient<MoviesPage>();
        builder.Services.AddTransient<MovieDetailsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}