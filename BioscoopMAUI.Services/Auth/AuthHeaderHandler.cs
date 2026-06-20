using System.Net;
using System.Net.Http.Headers;
using BioscoopMAUI.Interfaces.Auth;

namespace BioscoopMAUI.Services.Auth;

public class AuthHeaderHandler(IAuthService authService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var accessToken = await authService.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized)
            await authService.HandleUnauthorizedAsync();

        return response;
    }
}