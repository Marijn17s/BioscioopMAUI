using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class LoginPageViewModel(IAuthService authService, INavigationService navigationService) : ObservableObject
{
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsLoginEnabled => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoginEnabled))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await authService.LoginAsync();
            await navigationService.GoToAsync($"//{NavigationRoutes.Home}");
        }
        catch (Exception exception)
        {
            ErrorMessage = GetLoginErrorMessage(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetLoginErrorMessage(Exception exception)
    {
        var message = exception.Message;

        if (message.Contains("cancel") || message.Contains("closed") || message.Contains("access_denied"))
            return "Sign in was cancelled. Please try again when you are ready.";

        return "We could not sign you in. Check your connection and try again.";
    }
}