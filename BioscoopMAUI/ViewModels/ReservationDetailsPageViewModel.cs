using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Models.Helpers;
using BioscoopMAUI.Navigation;
using BioscoopMAUI.ViewModels.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class ReservationDetailsPageViewModel(
    IReservationService reservationService,
    ITicketPdfService ticketPdfService,
    INavigationService navigationService,
    QrCodeHelper qrCodeHelper) : ObservableObject
{
    public ObservableCollection<SeatDto> Seats { get; } = [];
    public ObservableCollection<TicketQrCodeViewModel> TicketQrCodes { get; } = [];

    public bool HasReservation => Reservation is not null;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanManage => Reservation is { Status: ReservationStatus.Confirmed } && Reservation.Showtime.StartTime > DateTime.Now;

    private int _reservationId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasReservation))]
    [NotifyPropertyChangedFor(nameof(CanManage))]
    private ReservationResponseDto? _reservation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _seatSummary = "No seats";

    [ObservableProperty]
    private bool _isBusy;

    public async Task InitializeAsync(int reservationId)
    {
        _reservationId = reservationId;
        await LoadReservationAsync();
    }

    [RelayCommand]
    private async Task LoadReservationAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            Reservation = await reservationService.GetReservationAsync(_reservationId);
            Seats.Clear();
            TicketQrCodes.Clear();

            if (Reservation is null)
            {
                ErrorMessage = "This reservation could not be found.";
                return;
            }

            foreach (var seat in Reservation.Seats.OrderBy(seat => seat.Row).ThenBy(seat => seat.SeatNumber))
            {
                Seats.Add(seat);
                var qrCodeImageBase64 = qrCodeHelper.GetQrCodeImage(qrCodeHelper.GetSeatQrCodeString(Reservation, seat));
                var qrCodeImageBytes = Convert.FromBase64String(qrCodeImageBase64);
                TicketQrCodes.Add(new TicketQrCodeViewModel(
                    $"Row {seat.Row} Seat {seat.SeatNumber}",
                    ImageSource.FromStream(() => new MemoryStream(qrCodeImageBytes))));
            }

            SeatSummary = GetSeatSummaryText(Reservation.Seats);
        }
        catch (Exception)
        {
            ErrorMessage = "We can't load this reservation. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SharePdfAsync()
    {
        if (Reservation is null || IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var pdfPath = await ticketPdfService.GenerateReservationTicketsAsync(Reservation);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Share tickets",
                File = new ShareFile(pdfPath, "application/pdf")
            });
        }
        catch (Exception)
        {
            ErrorMessage = "We couldn't share or save these tickets. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ChangeSeatsAsync()
    {
        if (Reservation is null || !CanManage)
            return;

        await navigationService.GoToAsync(NavigationRoutes.SeatSelection, new Dictionary<string, object>
        {
            [NavigationRoutes.ShowtimeIdParameter] = Reservation.Showtime.Id,
            [NavigationRoutes.ReservationIdParameter] = Reservation.Id,
            [NavigationRoutes.MovieTitleParameter] = Reservation.MovieTitle,
            [NavigationRoutes.RoomNameParameter] = Reservation.RoomName,
            [NavigationRoutes.StartTimeParameter] = Reservation.Showtime.StartTime
        });
    }

    public async Task CancelReservationAsync()
    {
        if (Reservation is null)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            await reservationService.CancelReservationAsync(Reservation.Id);
            await LoadReservationAsync();
        }
        catch (Exception)
        {
            ErrorMessage = "We couldn't cancel this reservation. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string GetSeatSummaryText(IEnumerable<SeatDto> seats)
    {
        var groupedSeats = seats
            .GroupBy(seat => seat.Row)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var seatNumbers = string.Join(", ", group.OrderBy(seat => seat.SeatNumber).Select(seat => $"seat {seat.SeatNumber}"));
                return $"Row {group.Key}: {seatNumbers}";
            })
            .ToList();

        return groupedSeats.Count is 0 ? "No seats" : string.Join(Environment.NewLine, groupedSeats);
    }
}