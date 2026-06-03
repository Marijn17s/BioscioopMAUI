using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BioscoopMAUI.Interfaces.Navigation;
using BioscoopMAUI.Interfaces.Showtimes;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Navigation;
using BioscoopMAUI.ViewModels.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BioscoopMAUI.ViewModels;

public partial class ShowtimesPageViewModel : ObservableObject
{
    private const int PageSize = 15;
    private static readonly TimeSpan ScreeningLookAhead = TimeSpan.FromDays(14);

    private readonly IShowtimeService _showtimeService;
    private readonly INavigationService _navigationService;

    private List<FilmsOverviewDto> _allScreenings = [];
    private List<FilmsOverviewDto> _filteredScreenings = [];
    private int _displayedCount;
    private bool _hasLoadedOnce;
    private DateTime? _selectedDate;
    private bool _isUpdatingSelectedCalendarDate;

    public ShowtimesPageViewModel(IShowtimeService showtimeService, INavigationService navigationService)
    {
        _showtimeService = showtimeService;
        _navigationService = navigationService;

        Screenings.CollectionChanged += OnScreeningsChanged;
        InitializeFilterOptions();
    }

    public ObservableCollection<FilmsOverviewDto> Screenings { get; } = [];

    public ObservableCollection<ShowtimeFilterOption> DateFilterOptions { get; } = [];

    public ObservableCollection<ShowtimeFilterOption> TimeFilterOptions { get; } = [];

    public bool HasScreenings => Screenings.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmptyStateVisible => !IsBusy && !HasError && _hasLoadedOnce && _allScreenings.Count == 0;

    public bool ShowScreeningsList => _hasLoadedOnce && !HasError && !IsEmptyStateVisible;

    public bool IsInitialLoading => IsBusy && !_hasLoadedOnce;

    public bool HasLoadedOnce => _hasLoadedOnce;

    public DateTime MinimumFilterDate { get; } = DateTime.Today;

    public DateTime MaximumFilterDate { get; } = DateTime.Today.AddDays(ScreeningLookAhead.Days - 1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(ShowScreeningsList))]
    [NotifyPropertyChangedFor(nameof(IsInitialLoading))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(ShowScreeningsList))]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDateLabel))]
    [NotifyPropertyChangedFor(nameof(DateFilterChevron))]
    private bool _isDateFilterExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTimeLabel))]
    [NotifyPropertyChangedFor(nameof(TimeFilterChevron))]
    private bool _isTimeFilterExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedMovieLabel))]
    [NotifyPropertyChangedFor(nameof(MovieFilterChevron))]
    private bool _isMovieFilterExpanded;

    public string SelectedDateLabel => _selectedDate switch
    {
        null => "All",
        var date when date.Value.Date == DateTime.Today => "Today",
        var date when date.Value.Date == DateTime.Today.AddDays(1) => "Tomorrow",
        var date => date.Value.ToString("ddd d MMM")
    };

    public string SelectedTimeLabel => TimeFilterOptions.FirstOrDefault(option => option.IsSelected)?.Label ?? "All";

    public string SelectedMovieLabel => string.IsNullOrWhiteSpace(MovieSearchText) ? "All movies" : MovieSearchText;

    public string DateFilterChevron => IsDateFilterExpanded ? "Hide" : "Edit";

    public string TimeFilterChevron => IsTimeFilterExpanded ? "Hide" : "Edit";

    public string MovieFilterChevron => IsMovieFilterExpanded ? "Hide" : "Edit";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    [NotifyPropertyChangedFor(nameof(SelectedMovieLabel))]
    private string _movieSearchText = string.Empty;

    [ObservableProperty]
    private string _pendingMovieSearchText = string.Empty;

    [ObservableProperty]
    private DateTime _selectedCalendarDate = DateTime.Today;

    partial void OnSelectedCalendarDateChanged(DateTime value)
    {
        if (_isUpdatingSelectedCalendarDate)
            return;

        SetSelectedDate(value.Date, false);
    }

    partial void OnIsDateFilterExpandedChanged(bool value)
    {
        if (value)
        {
            IsTimeFilterExpanded = false;
            IsMovieFilterExpanded = false;
        }
    }

    partial void OnIsTimeFilterExpandedChanged(bool value)
    {
        if (value)
        {
            IsDateFilterExpanded = false;
            IsMovieFilterExpanded = false;
        }
    }

    partial void OnIsMovieFilterExpandedChanged(bool value)
    {
        if (value)
        {
            IsDateFilterExpanded = false;
            IsTimeFilterExpanded = false;
        }
    }

    [RelayCommand]
    private async Task LoadScreeningsAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var now = DateTime.Now;
            var maximumScreeningDate = now.Add(ScreeningLookAhead);
            _allScreenings = (await _showtimeService.GetUpcomingShowtimesAsync())
                .Where(screening => screening.StartTime >= now && screening.StartTime <= maximumScreeningDate)
                .OrderBy(screening => screening.StartTime)
                .ToList();
            _hasLoadedOnce = true;
            ApplyFiltersAndResetDisplay();
        }
        catch (Exception)
        {
            if (!_hasLoadedOnce)
                ErrorMessage = "We kunnen de voorstellingen nu niet laden. Controleer je verbinding en probeer opnieuw.";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsEmptyStateVisible));
            OnPropertyChanged(nameof(ShowScreeningsList));
            OnPropertyChanged(nameof(IsInitialLoading));
        }
    }

    [RelayCommand]
    private void LoadMore()
    {
        AppendNextChunk();
    }

    [RelayCommand]
    private void ToggleDateFilter()
    {
        IsDateFilterExpanded = !IsDateFilterExpanded;
    }

    [RelayCommand]
    private void ToggleTimeFilter()
    {
        IsTimeFilterExpanded = !IsTimeFilterExpanded;
    }

    [RelayCommand]
    private void ToggleMovieFilter()
    {
        IsMovieFilterExpanded = !IsMovieFilterExpanded;
    }

    [RelayCommand]
    private void ClearMovieSearch()
    {
        PendingMovieSearchText = string.Empty;
        MovieSearchText = string.Empty;
        ApplyFiltersAndResetDisplay();
        OnPropertyChanged(nameof(SelectedMovieLabel));
    }

    [RelayCommand]
    private void SearchMovie()
    {
        MovieSearchText = PendingMovieSearchText.Trim();
        ApplyFiltersAndResetDisplay();
        OnPropertyChanged(nameof(SelectedMovieLabel));
    }

    [RelayCommand]
    private void SelectDateFilter(ShowtimeFilterOption? chip)
    {
        if (chip is null)
            return;

        SetSelectedDate(chip.Date, false);
    }

    [RelayCommand]
    private void SelectTimeFilter(ShowtimeFilterOption? chip)
    {
        if (chip is null)
            return;

        foreach (var option in TimeFilterOptions)
            option.IsSelected = option.Key == chip.Key;

        IsTimeFilterExpanded = false;
        OnPropertyChanged(nameof(SelectedTimeLabel));
        ApplyFiltersAndResetDisplay();
    }

    [RelayCommand]
    private async Task OpenScreeningDetailsAsync(FilmsOverviewDto? screening)
    {
        if (screening is null)
            return;

        await _navigationService.GoToAsync(NavigationRoutes.ShowtimeDetails,
            new Dictionary<string, object>
            {
                [NavigationRoutes.ShowtimeIdParameter] = screening.ShowtimeId,
                [NavigationRoutes.MovieIdParameter] = screening.MovieId
            });
    }

    private void InitializeFilterOptions()
    {
        DateFilterOptions.Clear();
        DateFilterOptions.Add(new ShowtimeFilterOption("All", "All", null) { IsSelected = true });
        DateFilterOptions.Add(new ShowtimeFilterOption("Today", "Today", DateTime.Today));
        DateFilterOptions.Add(new ShowtimeFilterOption("Tomorrow", "Tomorrow", DateTime.Today.AddDays(1)));

        TimeFilterOptions.Clear();
        TimeFilterOptions.Add(new ShowtimeFilterOption("All", "All", null) { IsSelected = true });
        TimeFilterOptions.Add(new ShowtimeFilterOption("Morning", "Morning", null));
        TimeFilterOptions.Add(new ShowtimeFilterOption("Afternoon", "Afternoon", null));
        TimeFilterOptions.Add(new ShowtimeFilterOption("Evening", "Evening", null));
    }

    private void ApplyFiltersAndResetDisplay()
    {
        _filteredScreenings = ApplyFilters(_allScreenings);
        ResetDisplayedScreenings();
        OnPropertyChanged(nameof(IsEmptyStateVisible));
    }

    private List<FilmsOverviewDto> ApplyFilters(IReadOnlyList<FilmsOverviewDto> screenings)
    {
        var selectedTime = TimeFilterOptions.FirstOrDefault(option => option.IsSelected);

        IEnumerable<FilmsOverviewDto> filtered = screenings;

        if (_selectedDate is DateTime targetDate)
        {
            filtered = filtered.Where(screening => screening.StartTime.Date == targetDate.Date);
        }

        if (selectedTime?.Key is "Morning")
        {
            filtered = filtered.Where(screening => screening.StartTime.TimeOfDay < TimeSpan.FromHours(12));
        }
        else if (selectedTime?.Key is "Afternoon")
        {
            filtered = filtered.Where(screening =>
                screening.StartTime.TimeOfDay >= TimeSpan.FromHours(12)
                && screening.StartTime.TimeOfDay < TimeSpan.FromHours(17));
        }
        else if (selectedTime?.Key is "Evening")
        {
            filtered = filtered.Where(screening => screening.StartTime.TimeOfDay >= TimeSpan.FromHours(17));
        }

        if (!string.IsNullOrWhiteSpace(MovieSearchText))
        {
            filtered = filtered.Where(screening =>
                screening.Title.Contains(MovieSearchText, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    private void ResetDisplayedScreenings()
    {
        _displayedCount = 0;
        Screenings.Clear();
        AppendNextChunk();
    }

    private void SetSelectedDate(DateTime? date, bool closeFilter)
    {
        _selectedDate = date?.Date;

        foreach (var option in DateFilterOptions)
            option.IsSelected = option.Date?.Date == _selectedDate;

        if (_selectedDate is null)
        {
            DateFilterOptions.First(option => option.Date is null).IsSelected = true;
        }
        else
        {
            UpdateSelectedCalendarDate(_selectedDate.Value);
        }

        if (closeFilter)
            IsDateFilterExpanded = false;

        OnPropertyChanged(nameof(SelectedDateLabel));
        ApplyFiltersAndResetDisplay();
    }

    private void UpdateSelectedCalendarDate(DateTime date)
    {
        _isUpdatingSelectedCalendarDate = true;
        SelectedCalendarDate = date;
        _isUpdatingSelectedCalendarDate = false;
    }

    private void AppendNextChunk()
    {
        var remaining = _filteredScreenings.Count - _displayedCount;
        if (remaining <= 0)
            return;

        var count = Math.Min(PageSize, remaining);
        for (var index = _displayedCount; index < _displayedCount + count; index++)
            Screenings.Add(_filteredScreenings[index]);

        _displayedCount += count;
    }

    private void OnScreeningsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasScreenings));
    }
}