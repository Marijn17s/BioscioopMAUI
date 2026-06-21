#if UITEST
using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Models.Auth;

namespace BioscoopMAUI.Services.Auth;

public class FakeAuthService : IAuthService
{
    public event EventHandler? SessionExpired;

    public AuthenticatedUser? CurrentUser { get; private set; } = new(
        "auth0|uitest-user",
        "uitest@example.com",
        "UI Test User",
        AuthConstants.UserRole);

    public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);

    public Task<string?> GetAccessTokenAsync() => Task.FromResult<string?>("uitest-access-token");

    public Task LoginAsync() => Task.CompletedTask;

    public Task LogoutAsync()
    {
        CurrentUser = null;
        return Task.CompletedTask;
    }

    public Task HandleUnauthorizedAsync()
    {
        SessionExpired?.Invoke(this, EventArgs.Empty);
        CurrentUser = null;
        return Task.CompletedTask;
    }
}
#endif