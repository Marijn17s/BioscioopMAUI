using BioscoopMAUI.ViewModels;
using BarcodeScanning;

namespace BioscoopMAUI.Views;

public partial class TicketScannerPage
{
    private readonly TicketScannerPageViewModel _viewModel;

    public TicketScannerPage(TicketScannerPageViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        await Methods.AskForRequiredPermissionAsync();
    }

    protected override void OnDisappearing()
    {
        TicketCameraView.CameraEnabled = false;
        base.OnDisappearing();
    }

    private async void OnDetectionFinished(object? sender, OnDetectionFinishedEventArg e)
    {
        if (TicketCameraView.PauseScanning)
            return;

        var qrCode = e.BarcodeResults.FirstOrDefault()?.DisplayValue;
        if (string.IsNullOrWhiteSpace(qrCode))
            return;

        await _viewModel.HandleQrCodeDetectedAsync(qrCode);
    }
}