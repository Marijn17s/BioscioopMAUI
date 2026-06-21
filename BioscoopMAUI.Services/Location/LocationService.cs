using BioscoopMAUI.Interfaces.Location;
using BioscoopMAUI.Models.Constants;
using Shiny.Locations;

namespace BioscoopMAUI.Services.Location;

public class LocationService(IGeofenceManager geofenceManager) : ILocationService
{
    public const string CinemaRegionIdentifier = "cinema-proximity";
    private static readonly GeofenceRegion Region = new(CinemaRegionIdentifier, new Position(CinemaLocation.Latitude, CinemaLocation.Longitude), Distance.FromKilometers(CinemaLocation.ProximityRadiusKilometers));

    public async Task<bool> RequestPermissionAsync()
    {
        var access = await geofenceManager.RequestAccess();
        return access is AccessState.Available;
    }

    public async Task StartMonitoringAsync()
        => await geofenceManager.TryStartMonitoring(Region);

    public async Task StopMonitoringAsync()
        => await geofenceManager.StopMonitoring(CinemaRegionIdentifier);

    public async Task<bool> IsInsideCinemaRegionAsync()
    {
        var state = await geofenceManager.RequestState(Region);
        return state is GeofenceState.Entered;
    }
}