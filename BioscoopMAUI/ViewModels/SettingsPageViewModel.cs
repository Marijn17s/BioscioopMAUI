using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Interfaces.Feedback;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class SettingsPageViewModel(
    IAuthService authService,
    INavigationService navigationService,
    IFeedbackService feedbackService) : ObservableObject
{
    public bool IsEmployee => string.Equals(Role, AuthConstants.EmployeeRole);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasFeedbackSuccess => !string.IsNullOrWhiteSpace(FeedbackSuccessMessage);
    public bool IsFeedbackInputEnabled => !IsBusy;
    public bool CanSubmitFeedback => !IsBusy && !string.IsNullOrWhiteSpace(FeedbackText);

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
    [NotifyPropertyChangedFor(nameof(IsFeedbackInputEnabled))]
    [NotifyPropertyChangedFor(nameof(CanSubmitFeedback))]
    [NotifyCanExecuteChangedFor(nameof(SubmitFeedbackCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmitFeedback))]
    [NotifyCanExecuteChangedFor(nameof(SubmitFeedbackCommand))]
    private string _feedbackText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFeedbackSuccess))]
    private string _feedbackSuccessMessage = string.Empty;

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

    [RelayCommand(CanExecute = nameof(CanSubmitFeedback))]
    private async Task SubmitFeedbackAsync()
    {
        if (IsBusy)
            return;

        var feedback = FeedbackText.Trim();
        if (string.IsNullOrWhiteSpace(feedback))
        {
            ErrorMessage = "Please enter your feedback before submitting.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        FeedbackSuccessMessage = string.Empty;

        try
        {
            await feedbackService.SubmitFeedbackAsync(feedback);
            FeedbackText = string.Empty;
            FeedbackSuccessMessage = "Thank you for your feedback.";
        }
        catch
        {
            ErrorMessage = "We couldn't send your feedback. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenTicketScannerAsync()
    {
        if (!IsEmployee || IsBusy)
            return;

        await navigationService.GoToAsync(NavigationRoutes.TicketScanner);
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