using BioscoopMAUI.Interfaces.Navigation;

namespace BioscoopMAUI.Services.Navigation;

public class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        => parameters is null ? Shell.Current.GoToAsync(route) : Shell.Current.GoToAsync(route, parameters);
}