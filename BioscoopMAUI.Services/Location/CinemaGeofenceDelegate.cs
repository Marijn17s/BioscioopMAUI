using BioscoopMAUI.Interfaces.Notifications;
using Shiny.Locations;

namespace BioscoopMAUI.Services.Location;

public class CinemaGeofenceDelegate(INotificationService notificationService) : IGeofenceDelegate
{
    public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
    {
        if (region.Identifier is not LocationService.CinemaRegionIdentifier)
            return;

        if (newStatus is GeofenceState.Entered)
            await notificationService.ScheduleStoredNotificationsAsync();
        else if (newStatus is GeofenceState.Exited)
            notificationService.CancelScheduledNotifications();
    }
}