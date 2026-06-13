using System.Collections.ObjectModel;

namespace BioscoopMAUI.ViewModels.UI;

public class SeatRowViewModel(int rowNumber)
{
    public int RowNumber { get; } = rowNumber;

    public ObservableCollection<SeatOptionViewModel> Seats { get; } = [];
}