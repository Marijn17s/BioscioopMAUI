using System.Security.Claims;
using BioscoopMAUI.Models.Auth;

namespace BioscoopMAUI.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetAuth0UserId(this ClaimsPrincipal user) => user.FindFirstValue(AuthConstants.Auth0UserIdClaimType);
}