using BioscoopMAUI.Interfaces.Notifications;
using BioscoopMAUI.Interfaces.Reservations;

namespace BioscoopMAUI;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = _services.GetRequiredService<AppShell>();
        return new Window(shell);
    }

    protected override void OnResume()
    {
        base.OnResume();

        var notificationService = _services.GetService<INotificationService>();
        if (notificationService is null || !notificationService.AreNotificationsEnabled)
            return;

        var reservationService = _services.GetRequiredService<IReservationService>();
        _ = reservationService.GetReservationsAsync();
    }
}