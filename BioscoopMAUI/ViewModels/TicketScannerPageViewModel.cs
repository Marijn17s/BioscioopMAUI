using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Constants;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class TicketScannerPageViewModel(IAuthService authService, IReservationService reservationService, INavigationService navigationService) : ObservableObject
{
    public bool HasResult => !string.IsNullOrWhiteSpace(ResultTitle);
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsScannerActive => !IsBusy && !HasResult;
    public bool IsScanningPaused => IsBusy || HasResult;
    public bool IsValidating => IsBusy && !HasResult;
    public bool HasTicketDetails => !string.IsNullOrWhiteSpace(TicketDetails);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsScannerActive))]
    [NotifyPropertyChangedFor(nameof(IsScanningPaused))]
    [NotifyPropertyChangedFor(nameof(IsValidating))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyPropertyChangedFor(nameof(IsScannerActive))]
    [NotifyPropertyChangedFor(nameof(IsScanningPaused))]
    [NotifyPropertyChangedFor(nameof(IsValidating))]
    private string _resultTitle = string.Empty;

    [ObservableProperty]
    private string _resultIcon = string.Empty;

    [ObservableProperty]
    private Color _resultColor = Colors.Transparent;

    [ObservableProperty]
    private string _resultMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTicketDetails))]
    private string _ticketDetails = string.Empty;

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

        if (!string.Equals(authService.CurrentUser?.Role, AuthConstants.EmployeeRole))
            await navigationService.GoToAsync($"//{NavigationRoutes.Settings}");
    }

    public async Task HandleQrCodeDetectedAsync(string qrCode)
    {
        if (IsBusy || HasResult || string.IsNullOrWhiteSpace(qrCode))
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var validationResult = await reservationService.ValidateQrCodeAsync(qrCode);
            ShowResult(validationResult);
        }
        catch
        {
            ResultIcon = TabIconGlyphs.Cancel;
            ResultTitle = "Failed to validate";
            ResultColor = Colors.DarkRed;
            ResultMessage = "We couldn't validate this ticket. Check your connection and try again.";
            TicketDetails = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ScanNextTicket()
    {
        ResultIcon = string.Empty;
        ResultTitle = string.Empty;
        ResultColor = Colors.Transparent;
        ResultMessage = string.Empty;
        TicketDetails = string.Empty;
        ErrorMessage = string.Empty;
    }

    private void ShowResult(QrCodeValidationResponseDto validationResult)
    {
        ResultIcon = validationResult.IsValid ? TabIconGlyphs.CheckCircle : TabIconGlyphs.Cancel;
        ResultTitle = validationResult.IsValid ? "Valid ticket" : "Invalid ticket";
        ResultColor = validationResult.IsValid ? Colors.Green : Colors.Red;
        ResultMessage = validationResult.IsValid ? "This ticket is approved for entry." : validationResult.ErrorMessage ?? "This ticket is invalid.";
        TicketDetails = GetTicketDetails(validationResult);
    }

    private static string GetTicketDetails(QrCodeValidationResponseDto validationResult)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(validationResult.MovieTitle))
            details.Add(validationResult.MovieTitle);

        if (validationResult.ShowtimeStartTime is not null)
            details.Add(validationResult.ShowtimeStartTime.Value.ToString("dddd d MMMM yyyy HH:mm"));

        if (!string.IsNullOrWhiteSpace(validationResult.RoomName))
            details.Add(validationResult.RoomName);

        if (validationResult.Seats is { Count: > 0 })
        {
            var seats = validationResult.Seats
                .OrderBy(seat => seat.Row)
                .ThenBy(seat => seat.SeatNumber)
                .Select(seat => $"Row {seat.Row}, Seat {seat.SeatNumber}");
            details.Add(string.Join(Environment.NewLine, seats));
        }

        return string.Join(Environment.NewLine, details);
    }
}