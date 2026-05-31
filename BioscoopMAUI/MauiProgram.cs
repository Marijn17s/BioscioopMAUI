using System.Reflection;
using Auth0.OidcClient;
using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Interfaces.Movies;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Models.Configuration;
using BioscoopMAUI.Services.Auth;
using BioscoopMAUI.Services.Movies;
using BioscoopMAUI.Services.Navigation;
using BioscoopMAUI.ViewModels;
using BioscoopMAUI.Views;
using Microsoft.Extensions.Configuration;
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

        var assembly = Assembly.GetExecutingAssembly();
        var resourcePrefix = assembly.GetName().Name;

        using (var stream = assembly.GetManifestResourceStream($"{resourcePrefix}.appsettings.json")
            ?? throw new InvalidOperationException("appsettings.json not found."))
        {
            builder.Configuration.AddJsonStream(stream);
        }

        using (var developmentStream = assembly.GetManifestResourceStream($"{resourcePrefix}.appsettings.Development.json"))
        {
            if (developmentStream is not null)
                builder.Configuration.AddJsonStream(developmentStream);
        }

        var auth0Settings = builder.Configuration
            .GetSection(Auth0Settings.SectionName)
            .Get<Auth0Settings>() ?? throw new InvalidOperationException($"Configuration section '{Auth0Settings.SectionName}' not found.");

        if (string.IsNullOrWhiteSpace(auth0Settings.Domain)
            || string.IsNullOrWhiteSpace(auth0Settings.ClientId)
            || string.IsNullOrWhiteSpace(auth0Settings.Audience)
            || string.IsNullOrWhiteSpace(auth0Settings.RedirectUri)
            || string.IsNullOrWhiteSpace(auth0Settings.PostLogoutRedirectUri)
            || string.IsNullOrWhiteSpace(auth0Settings.Scope))
        {
            throw new InvalidOperationException("Auth0:Domain, Auth0:ClientId, Auth0:Audience, Auth0:RedirectUri, Auth0:PostLogoutRedirectUri, and Auth0:Scope are required. ");
        }

        var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("Api:BaseUrl is not configured. ");

        builder.Services.AddSingleton(auth0Settings);

        builder.Services.AddSingleton(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<Auth0Settings>();
            return new Auth0Client(new Auth0ClientOptions
            {
                Domain = settings.Domain,
                ClientId = settings.ClientId,
                Scope = settings.Scope,
                RedirectUri = settings.RedirectUri,
                PostLogoutRedirectUri = settings.PostLogoutRedirectUri
            });
        });

        builder.Services.AddSingleton<IAuthService, Auth0AuthService>();
        builder.Services.AddTransient<AuthHeaderHandler>();

        builder.Services.AddHttpClient("BioscoopAPI", client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
        }).AddHttpMessageHandler<AuthHeaderHandler>();

        builder.Services.AddSingleton<INavigationService, ShellNavigationService>();
        builder.Services.AddSingleton<IMovieService, MovieService>();

        builder.Services.AddSingleton<AppShell>();

        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MoviesPageViewModel>();
        builder.Services.AddTransient<MovieDetailsPageViewModel>();
        builder.Services.AddTransient<LoginPageViewModel>();
        builder.Services.AddTransient<SettingsPageViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<MoviesPage>();
        builder.Services.AddTransient<MovieDetailsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}