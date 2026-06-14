using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Notifications;

public interface INotificationService
{
    bool AreRemindersEnabled { get; }
    Task<bool> EnableAsync();
    void Disable();
    Task SyncRemindersAsync(IEnumerable<ReservationResponseDto> reservations);
}