using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Notifications;

public interface INotificationService
{
    bool AreNotificationsEnabled { get; }
    Task<bool> EnableAsync();
    Task DisableAsync();
    Task SyncNotificationsAsync(IEnumerable<ReservationResponseDto> reservations);
    Task ScheduleStoredNotificationsAsync();
    void CancelScheduledNotifications();
}