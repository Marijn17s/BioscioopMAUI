using CommunityToolkit.Mvvm.ComponentModel;

namespace BioscoopMAUI.ViewModels.UI;

public partial class SeatOptionViewModel(int id, int row, int seatNumber, bool isAvailable) : ObservableObject
{
    private static readonly Color AvailableColor = Color.FromArgb("#24A148");
    private static readonly Color SelectedColor = Color.FromArgb("#FFC107");
    private static readonly Color SuggestedColor = Color.FromArgb("#3DDC84");
    private static readonly Color UnavailableColor = Color.FromArgb("#D73A49");

    public int Id { get; } = id;

    public int Row { get; } = row;

    public int SeatNumber { get; } = seatNumber;

    public bool IsAvailable { get; } = isAvailable;

    public Color BackgroundColor
    {
        get
        {
            if (!IsAvailable)
                return UnavailableColor;
            if (IsSelected)
                return SelectedColor;
            if (IsSuggested)
                return SuggestedColor;
            return AvailableColor;
        }
    }

    public Color TextColor => IsSelected ? Colors.Black : Colors.White;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundColor))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    private bool _isSelected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundColor))]
    private bool _isSuggested;
}