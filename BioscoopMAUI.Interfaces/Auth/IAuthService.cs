using BioscoopMAUI.Models.Auth;

namespace BioscoopMAUI.Interfaces.Auth;

public interface IAuthService
{
    event EventHandler? SessionExpired;

    Task<bool> IsAuthenticatedAsync();

    AuthenticatedUser? CurrentUser { get; }

    Task<string?> GetAccessTokenAsync();

    Task LoginAsync();

    Task LogoutAsync();

    Task HandleUnauthorizedAsync();
}