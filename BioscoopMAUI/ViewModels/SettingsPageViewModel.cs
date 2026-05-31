using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class SettingsPageViewModel(IAuthService authService, INavigationService navigationService) : ObservableObject
{
    public bool IsEmployee => string.Equals(Role, AuthConstants.EmployeeRole, StringComparison.OrdinalIgnoreCase);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmployee))]
    private string _email = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmployee))]
    private string _displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmployee))]
    private string _role = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    public async Task InitializeAsync()
    {
        ErrorMessage = string.Empty;

        if (!await authService.IsAuthenticatedAsync())
        {
            await navigationService.GoToAsync($"//{NavigationRoutes.Login}");
            return;
        }

        var user = authService.CurrentUser;
        if (user is null)
            return;

        Email = user.Email;
        DisplayName = user.DisplayName;
        Role = user.Role;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await authService.LogoutAsync();
            await navigationService.GoToAsync($"//{NavigationRoutes.Login}");
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}