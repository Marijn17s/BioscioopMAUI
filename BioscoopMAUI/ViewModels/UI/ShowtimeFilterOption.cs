using CommunityToolkit.Mvvm.ComponentModel;

namespace BioscoopMAUI.ViewModels.UI;

public partial class ShowtimeFilterOption(string label, string key, DateTime? date) : ObservableObject
{
    public string Label { get; } = label;

    public string Key { get; } = key;

    public DateTime? Date { get; } = date;

    [ObservableProperty]
    private bool _isSelected;
}