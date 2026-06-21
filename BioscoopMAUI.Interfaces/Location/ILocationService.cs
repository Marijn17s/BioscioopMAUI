namespace BioscoopMAUI.Interfaces.Location;

public interface ILocationService
{
    Task<bool> RequestPermissionAsync();
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    Task<bool> IsInsideCinemaRegionAsync();
}