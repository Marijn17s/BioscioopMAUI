namespace BioscoopMAUI.Interfaces.Auth;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();

    AuthenticatedUser? CurrentUser { get; }

    Task<string?> GetAccessTokenAsync();

    Task LoginAsync();

    Task LogoutAsync();

    Task<bool> TryRefreshAccessTokenAsync();
}