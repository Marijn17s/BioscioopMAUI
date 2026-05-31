using BioscoopMAUI.ViewModels;

namespace BioscoopMAUI.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginPageViewModel _viewModel;

    public LoginPage(LoginPageViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Shell.SetTabBarIsVisible(this, false);
    }
}