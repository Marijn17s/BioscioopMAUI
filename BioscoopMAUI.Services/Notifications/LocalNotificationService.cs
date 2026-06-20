using BioscoopMAUI.Models.DTOs;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using INotificationService = BioscoopMAUI.Interfaces.Notifications.INotificationService;

namespace BioscoopMAUI.Services.Notifications;

public class LocalNotificationService : INotificationService
{
    public const string ReminderChannelId = "showtime_reminders";

    private const string RemindersEnabledPreferenceKey = "showtime_reminders_enabled";
    private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromMinutes(15);

    public bool AreRemindersEnabled => Preferences.Default.Get(RemindersEnabledPreferenceKey, false);

    public async Task<bool> EnableAsync()
    {
        var permissionRequest = new NotificationPermission
        {
            Android =
            {
                RequestPermissionToScheduleExactAlarm = true
            }
        };

        var permissionGranted = await LocalNotificationCenter.Current.RequestNotificationPermission(permissionRequest);
        Preferences.Default.Set(RemindersEnabledPreferenceKey, permissionGranted);
        return permissionGranted;
    }

    public void Disable()
    {
        Preferences.Default.Set(RemindersEnabledPreferenceKey, false);
        LocalNotificationCenter.Current.CancelAll();
    }

    public async Task SyncRemindersAsync(IEnumerable<ReservationResponseDto> reservations)
    {
        if (!AreRemindersEnabled)
            return;

        LocalNotificationCenter.Current.CancelAll();

        var now = DateTimeOffset.Now;

        foreach (var reservation in reservations.Where(reservation => reservation.Status == ReservationStatus.Confirmed))
        {
            var showtimeStart = new DateTimeOffset(reservation.Showtime.StartTime);
            var notifyTime = showtimeStart - ReminderLeadTime;
            if (notifyTime <= now)
                continue;

            var notification = new NotificationRequest
            {
                NotificationId = reservation.Id,
                Title = reservation.MovieTitle,
                Description = $"Starts at {reservation.Showtime.StartTime:HH:mm} in {reservation.RoomName}.",
                Android =
                {
                    ChannelId = ReminderChannelId
                },
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = notifyTime
                }
            };

            await LocalNotificationCenter.Current.Show(notification);
        }
    }
}