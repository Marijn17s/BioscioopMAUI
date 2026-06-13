using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class ReservationsPageViewModel(
    IReservationService reservationService,
    INavigationService navigationService) : ObservableObject
{
    public ObservableCollection<ReservationResponseDto> ActiveReservations { get; } = [];
    public ObservableCollection<ReservationResponseDto> ReservationHistory { get; } = [];

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsEmptyStateVisible => !IsBusy && !HasError && !HasActiveReservations && !HasReservationHistory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private bool _hasActiveReservations;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private bool _hasReservationHistory;

    [RelayCommand]
    public async Task LoadReservationsAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var reservations = (await reservationService.GetReservationsAsync())
                .OrderBy(reservation => reservation.Showtime.StartTime)
                .ToList();

            ActiveReservations.Clear();
            ReservationHistory.Clear();

            foreach (var reservation in reservations)
            {
                if (reservation.Status is ReservationStatus.Confirmed && reservation.Showtime.StartTime >= DateTime.Now)
                    ActiveReservations.Add(reservation);
                else
                    ReservationHistory.Add(reservation);
            }

            HasActiveReservations = ActiveReservations.Count > 0;
            HasReservationHistory = ReservationHistory.Count > 0;
        }
        catch (Exception)
        {
            ErrorMessage = "We can't load your reservations. Please check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenReservationAsync(ReservationResponseDto reservation)
    {
        await navigationService.GoToAsync(NavigationRoutes.ReservationDetails, new Dictionary<string, object>
        {
            [NavigationRoutes.ReservationIdParameter] = reservation.Id
        });
    }
}