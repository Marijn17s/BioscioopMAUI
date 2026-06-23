using System.Text.Json;
using BioscoopMAUI.Interfaces.Location;
using BioscoopMAUI.Models.DTOs;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using INotificationService = BioscoopMAUI.Interfaces.Notifications.INotificationService;

namespace BioscoopMAUI.Services.Notifications;

public class LocalNotificationService(ILocationService locationService) : INotificationService
{
    public const string NotificationChannelId = "showtime_notifications";
    private const string NotificationsEnabledPreferenceKey = "showtime_notifications_enabled";
    private const string StoredNotificationsPreferenceKey = "showtime_notifications_pending";
    private static readonly TimeSpan NotificationTimeSpan = TimeSpan.FromMinutes(15);

    public bool AreNotificationsEnabled => Preferences.Default.Get(NotificationsEnabledPreferenceKey, false);

    public async Task<bool> EnableAsync()
    {
        var permissionRequest = new NotificationPermission
        {
            Android =
            {
                RequestPermissionToScheduleExactAlarm = true
            }
        };

        var notificationPermissionGranted = await LocalNotificationCenter.Current.RequestNotificationPermission(permissionRequest);
        if (!notificationPermissionGranted)
        {
            Preferences.Default.Set(NotificationsEnabledPreferenceKey, false);
            return false;
        }

        var locationPermissionGranted = await locationService.RequestPermissionAsync();
        Preferences.Default.Set(NotificationsEnabledPreferenceKey, locationPermissionGranted);
        return locationPermissionGranted;
    }

    public async Task DisableAsync()
    {
        Preferences.Default.Set(NotificationsEnabledPreferenceKey, false);
        Preferences.Default.Remove(StoredNotificationsPreferenceKey);
        await locationService.StopMonitoringAsync();
        LocalNotificationCenter.Current.CancelAll();
    }

    public async Task SyncNotificationsAsync(IEnumerable<ReservationResponseDto> reservations)
    {
        if (!AreNotificationsEnabled)
            return;

        var pendingNotifications = reservations
            .Where(reservation => reservation.Status == ReservationStatus.Confirmed)
            .Where(reservation => reservation.Showtime.StartTime > DateTime.Now)
            .Select(reservation => new PendingNotification(
                reservation.Id,
                reservation.MovieTitle,
                reservation.Showtime.StartTime,
                reservation.RoomName))
            .ToList();

        SaveStoredNotifications(pendingNotifications);

        // Notifications trigger even if the app is closed
        await locationService.StartMonitoringAsync();

        if (await locationService.IsInsideCinemaRegionAsync())
            await ScheduleStoredNotificationsAsync();
        else
            CancelScheduledNotifications();
    }

    public async Task ScheduleStoredNotificationsAsync()
    {
        if (!AreNotificationsEnabled)
            return;

        var now = DateTime.Now;

        foreach (var pendingNotification in GetStoredNotifications())
        {
            if (pendingNotification.ShowtimeStart <= now)
                continue;

            var notifyTime = pendingNotification.ShowtimeStart - NotificationTimeSpan;
            if (notifyTime < now)
                notifyTime = now.AddSeconds(5);

            var notification = new NotificationRequest
            {
                NotificationId = pendingNotification.ReservationId,
                Title = pendingNotification.MovieTitle,
                Description = $"Starts at {pendingNotification.ShowtimeStart:HH:mm} in {pendingNotification.RoomName}.",
                Android =
                {
                    ChannelId = NotificationChannelId
                },
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
        }
    }

    public void CancelScheduledNotifications() => LocalNotificationCenter.Current.CancelAll();

    private static List<PendingNotification> GetStoredNotifications()
    {
        var storedValue = Preferences.Default.Get(StoredNotificationsPreferenceKey, string.Empty);
        if (string.IsNullOrWhiteSpace(storedValue))
            return [];

        return JsonSerializer.Deserialize<List<PendingNotification>>(storedValue) ?? [];
    }

    private static void SaveStoredNotifications(List<PendingNotification> pendingNotifications)
    {
        var storedValue = JsonSerializer.Serialize(pendingNotifications);
        Preferences.Default.Set(StoredNotificationsPreferenceKey, storedValue);
    }

    private record PendingNotification(
        int ReservationId,
        string MovieTitle,
        DateTime ShowtimeStart,
        string RoomName);
}