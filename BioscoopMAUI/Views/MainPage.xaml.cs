using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class MainPage : ContentPage
{
    int _count;

    public MainPage(MainPageViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    private void OnCounterClicked(object? sender, EventArgs e)
    {
        _count++;

        CounterBtn.Text = _count == 1 ? $"Clicked {_count} time" : $"Clicked {_count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}