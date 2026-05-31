namespace BioscoopMAUI.Interfaces.Auth;

public record AuthenticatedUser(
    string UserId,
    string Email,
    string DisplayName,
    string Role);