using System.Collections.ObjectModel;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Navigation;
using BioscoopMAUI.ViewModels.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class SeatSelectionPageViewModel(
    ISeatSelectionService seatSelectionService,
    IPaymentService paymentService,
    IReservationService reservationService,
    INavigationService navigationService) : ObservableObject
{
    public ObservableCollection<SeatRowViewModel> Rows { get; } = [];

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasDiscount => PriceQuote?.DiscountPercentage > 0;
    public bool IsEditMode => ReservationId > 0;
    public bool CanEditTicketCount => !IsEditMode && !IsBusy;
    public bool CanConfirm => SelectedSeatCount == TicketCount && !IsBusy;
    public string ConfirmButtonText => IsEditMode ? "Save seats" : $"Confirm ({SelectedSeatCount} / {TicketCount})";
    public string PriceText => PriceQuote is null ? "Price unavailable" : $"Total: {PriceQuote.TotalPrice:C}";
    public string DiscountText => PriceQuote is null ? string.Empty : $"{PriceQuote.DiscountPercentage:0}% off";
    public string Subtitle => $"{MovieTitle} - {StartTime:dddd d MMMM HH:mm}";

    public int ShowtimeId { get; private set; }
    private readonly HashSet<int> _currentReservationSeatIds = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(CanEditTicketCount))]
    private int _reservationId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    private string _movieTitle = string.Empty;

    [ObservableProperty]
    private string _roomName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtitle))]
    private DateTime _startTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditTicketCount))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    [NotifyPropertyChangedFor(nameof(ConfirmButtonText))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    [NotifyPropertyChangedFor(nameof(ConfirmButtonText))]
    private int _ticketCount = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    [NotifyPropertyChangedFor(nameof(ConfirmButtonText))]
    private int _selectedSeatCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceText))]
    [NotifyPropertyChangedFor(nameof(DiscountText))]
    [NotifyPropertyChangedFor(nameof(HasDiscount))]
    private PriceQuoteDto? _priceQuote;

    public async Task InitializeAsync(int showtimeId, string movieTitle, string roomName, DateTime startTime, int? reservationId = null)
    {
        if (IsBusy)
            return;

        ShowtimeId = showtimeId;
        ReservationId = reservationId ?? 0;
        MovieTitle = movieTitle;
        RoomName = roomName;
        StartTime = startTime;
        _currentReservationSeatIds.Clear();

        if (IsEditMode)
        {
            var reservation = await reservationService.GetReservationAsync(ReservationId);
            TicketCount = reservation?.Seats.Count ?? TicketCount;
            if (reservation is not null)
            {
                foreach (var seat in reservation.Seats)
                    _currentReservationSeatIds.Add(seat.SeatId);
            }
        }

        await LoadSeatsAsync();
    }

    public async Task OnTicketCountStepperChangedAsync(int newTicketCount)
    {
        if (IsEditMode || IsBusy)
            return;
        
        if (newTicketCount == TicketCount)
            return;

        TicketCount = newTicketCount;
        await RefreshSuggestionAsync();
    }

    [RelayCommand]
    private void SelectSeat(SeatOptionViewModel seat)
    {
        if (!seat.IsAvailable || IsBusy)
            return;

        ErrorMessage = string.Empty;

        if (seat.IsSelected)
        {
            seat.IsSelected = false;
        }
        else
        {
            var selectedSeats = Rows.SelectMany(row => row.Seats).Where(item => item.IsSelected).ToList();
            if (selectedSeats.Count >= TicketCount)
            {
                ErrorMessage = $"You can select {TicketCount} seat(s). Deselect a seat before choosing another.";
                return;
            }

            seat.IsSelected = true;
        }

        UpdateSelectedSeatCount();
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (!CanConfirm)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;
        CreateSeatHoldResponseDto? hold = null;

        try
        {
            var selectedSeatIds = Rows
                .SelectMany(row => row.Seats)
                .Where(seat => seat.IsSelected)
                .Select(seat => seat.Id)
                .ToList();

            if (IsEditMode)
            {
                await reservationService.ChangeSeatsAsync(ReservationId, selectedSeatIds);
                await navigationService.GoToAsync(NavigationRoutes.ReservationDetails, new Dictionary<string, object>
                {
                    [NavigationRoutes.ReservationIdParameter] = ReservationId
                });
                return;
            }

            hold = await seatSelectionService.CreateHoldAsync(ShowtimeId, selectedSeatIds);
            var checkoutSession = await paymentService.CreateCheckoutSessionAsync(hold.HoldId);

            WebAuthenticatorResult callbackResult;
            try
            {
                callbackResult = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(checkoutSession.CheckoutUrl),
                    new Uri("bioscoopmaui://payment-return"));
            }
            catch (TaskCanceledException)
            {
                await ReleaseHoldAsync(hold.HoldId);
                ErrorMessage = "Payment was cancelled. Your seats were not reserved.";
                return;
            }

            if (string.Equals(GetQueryValue(callbackResult, "result"), "cancel", StringComparison.OrdinalIgnoreCase))
            {
                await ReleaseHoldAsync(hold.HoldId);
                ErrorMessage = "Payment was cancelled. Your seats were not reserved.";
                return;
            }

            var sessionId = GetQueryValue(callbackResult, "session_id");
            if (string.IsNullOrWhiteSpace(sessionId))
                sessionId = checkoutSession.SessionId;

            var paymentStatus = await paymentService.GetPaymentStatusAsync(sessionId);
            if (paymentStatus.ReservationId is null)
            {
                if (!string.Equals(paymentStatus.Status, "paid", StringComparison.OrdinalIgnoreCase))
                    await ReleaseHoldAsync(hold.HoldId);

                ErrorMessage = paymentStatus.ErrorMessage ?? "Payment is still being processed. Please refresh your reservations in a moment.";
                return;
            }

            await navigationService.GoToAsync(NavigationRoutes.ReservationDetails, new Dictionary<string, object>
            {
                [NavigationRoutes.ReservationIdParameter] = paymentStatus.ReservationId.Value
            });
        }
        catch (Exception)
        {
            if (hold is not null)
                await ReleaseHoldAsync(hold.HoldId);

            ErrorMessage = "We couldn't complete your reservation. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSeatsAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var seats = (await seatSelectionService.GetSeatMapAsync(ShowtimeId)).ToList();
            var suggestion = await seatSelectionService.SuggestSeatsAsync(ShowtimeId, TicketCount);
            var suggestedSeatIds = suggestion?.SuggestedSeats.Select(seat => seat.Id).ToHashSet() ?? [];
            var selectedSeatIds = IsEditMode ? _currentReservationSeatIds : suggestedSeatIds;

            Rows.Clear();
            foreach (var rowGroup in seats.GroupBy(seat => seat.Row).OrderBy(group => group.Key))
            {
                var row = new SeatRowViewModel(rowGroup.Key);
                foreach (var seat in rowGroup.OrderBy(seat => seat.SeatNumber))
                {
                    var isCurrentReservationSeat = _currentReservationSeatIds.Contains(seat.Id);
                    row.Seats.Add(new SeatOptionViewModel(seat.Id, seat.Row, seat.SeatNumber, seat.IsAvailable || isCurrentReservationSeat)
                    {
                        IsSuggested = !IsEditMode && suggestedSeatIds.Contains(seat.Id),
                        IsSelected = selectedSeatIds.Contains(seat.Id)
                    });
                }

                Rows.Add(row);
            }

            PriceQuote = await seatSelectionService.GetPriceQuoteAsync(ShowtimeId, TicketCount);
            UpdateSelectedSeatCount();
        }
        catch (Exception)
        {
            ErrorMessage = "We can't load seats for this screening. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshSuggestionAsync()
    {
        foreach (var seat in Rows.SelectMany(row => row.Seats))
        {
            seat.IsSelected = false;
            seat.IsSuggested = false;
        }

        UpdateSelectedSeatCount();
        await LoadSeatsAsync();
    }

    private void UpdateSelectedSeatCount()
    {
        SelectedSeatCount = Rows.SelectMany(row => row.Seats).Count(seat => seat.IsSelected);
    }

    private static string? GetQueryValue(WebAuthenticatorResult result, string key)
    {
        return result.Properties.GetValueOrDefault(key);
    }

    private async Task ReleaseHoldAsync(Guid holdId)
    {
        try
        {
            await seatSelectionService.ReleaseHoldAsync(holdId);
        }
        catch
        {
            // If it fails the API will release it later
        }
    }
}