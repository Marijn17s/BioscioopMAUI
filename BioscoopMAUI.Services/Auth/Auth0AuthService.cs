using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Auth0.OidcClient;
using BioscoopMAUI.Interfaces.Auth;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Models.Configuration;

namespace BioscoopMAUI.Services.Auth;

public class Auth0AuthService(Auth0Client auth0Client, Auth0Settings auth0Settings) : IAuthService
{
    private const string AccessTokenKey = "auth_access_token";
    private const string RefreshTokenKey = "auth_refresh_token";
    private const string UserIdKey = "auth_user_id";
    private const string EmailKey = "auth_email";
    private const string DisplayNameKey = "auth_display_name";
    private const string RoleKey = "auth_role";

    private static readonly string[] RoleClaimTypes =
    [
        AuthConstants.RolesClaimType,
        ClaimTypes.Role,
        "roles"
    ];

    public AuthenticatedUser? CurrentUser { get; private set; }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            CurrentUser = null;
            return false;
        }

        await LoadCurrentUserFromStorageAsync();
        return true;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
        await LoadCurrentUserFromStorageAsync();
        return accessToken;
    }

    public async Task LoginAsync()
    {
        var loginResult = await auth0Client.LoginAsync(new { audience = auth0Settings.Audience });

        if (loginResult.IsError)
            throw new InvalidOperationException(loginResult.Error ?? "Login failed");

        if (string.IsNullOrWhiteSpace(loginResult.AccessToken))
            throw new InvalidOperationException("Login did not return an access token");

        await StoreSessionAsync(
            loginResult.AccessToken,
            loginResult.RefreshToken,
            loginResult.User);
    }

    public Task LogoutAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(UserIdKey);
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(DisplayNameKey);
        SecureStorage.Default.Remove(RoleKey);
        CurrentUser = null;
        return Task.CompletedTask;
    }

    public async Task<bool> TryRefreshAccessTokenAsync()
    {
        var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var refreshResult = await auth0Client.RefreshTokenAsync(refreshToken);
        if (refreshResult.IsError || string.IsNullOrWhiteSpace(refreshResult.AccessToken))
            return false;

        await SecureStorage.Default.SetAsync(AccessTokenKey, refreshResult.AccessToken);

        if (!string.IsNullOrWhiteSpace(refreshResult.RefreshToken))
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshResult.RefreshToken);

        return true;
    }

    private async Task StoreSessionAsync(string accessToken, string? refreshToken, ClaimsPrincipal? user)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);

        if (!string.IsNullOrWhiteSpace(refreshToken))
            await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
        else
            SecureStorage.Default.Remove(RefreshTokenKey);

        var userId = user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;
        var email = user?.FindFirst(ClaimTypes.Email)?.Value
            ?? user?.FindFirst("email")?.Value
            ?? string.Empty;
        var displayName = user?.FindFirst("name")?.Value
            ?? user?.FindFirst(ClaimTypes.Name)?.Value
            ?? email;
        var role = ResolveRole(user, accessToken);

        await SecureStorage.Default.SetAsync(UserIdKey, userId);
        await SecureStorage.Default.SetAsync(EmailKey, email);
        await SecureStorage.Default.SetAsync(DisplayNameKey, displayName);
        await SecureStorage.Default.SetAsync(RoleKey, role);

        CurrentUser = new AuthenticatedUser(userId, email, displayName, role);
    }

    private async Task LoadCurrentUserFromStorageAsync()
    {
        if (CurrentUser is not null)
            return;

        var userId = await SecureStorage.Default.GetAsync(UserIdKey);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var email = await SecureStorage.Default.GetAsync(EmailKey) ?? string.Empty;
        var displayName = await SecureStorage.Default.GetAsync(DisplayNameKey) ?? email;
        var role = await SecureStorage.Default.GetAsync(RoleKey) ?? string.Empty;

        CurrentUser = new AuthenticatedUser(userId, email, displayName, role);
    }

    private static string ResolveRole(ClaimsPrincipal? identityTokenUser, string? accessToken)
    {
        var roles = GetRoles(identityTokenUser);

        if (roles.Count is 0 && !string.IsNullOrWhiteSpace(accessToken))
            roles = GetRoles(ParseTokenClaims(accessToken));

        return ResolvePrimaryRole(roles);
    }

    private static List<string> GetRoles(ClaimsPrincipal? user)
    {
        if (user is null)
            return [];

        var roles = new List<string>();

        foreach (var claimType in RoleClaimTypes)
        {
            foreach (var claim in user.FindAll(claimType))
                roles.AddRange(ParseClaimValue(claim.Value));
        }

        return roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IEnumerable<string> ParseClaimValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            yield break;

        var trimmedValue = value.Trim();
        if (trimmedValue.StartsWith('['))
        {
            var parsedRoles = TryParseJsonRoleArray(trimmedValue);
            if (parsedRoles is not null)
            {
                foreach (var role in parsedRoles)
                {
                    if (!string.IsNullOrWhiteSpace(role))
                        yield return role;
                }

                yield break;
            }
        }

        yield return trimmedValue;
    }

    private static string[]? TryParseJsonRoleArray(string jsonArray)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(jsonArray);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ResolvePrimaryRole(List<string> roles)
    {
        if (roles.Contains(AuthConstants.EmployeeRole, StringComparer.OrdinalIgnoreCase))
            return AuthConstants.EmployeeRole;

        if (roles.Contains(AuthConstants.UserRole, StringComparer.OrdinalIgnoreCase))
            return AuthConstants.UserRole;

        return roles.FirstOrDefault() ?? AuthConstants.UserRole;
    }

    private static ClaimsPrincipal? ParseTokenClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
            return null;

        var jwt = handler.ReadJwtToken(token);
        return new ClaimsPrincipal(new ClaimsIdentity(jwt.Claims));
    }
}