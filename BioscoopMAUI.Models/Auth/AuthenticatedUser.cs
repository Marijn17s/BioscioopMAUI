namespace BioscoopMAUI.Models.Auth;

public record AuthenticatedUser(
    string UserId,
    string Email,
    string DisplayName,
    string Role);