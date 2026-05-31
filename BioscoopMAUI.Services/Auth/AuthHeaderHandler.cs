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

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        if (!await authService.TryRefreshAccessTokenAsync())
            return response;

        var refreshedToken = await authService.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(refreshedToken))
            return response;

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedToken);
        response.Dispose();
        return await base.SendAsync(request, cancellationToken);
    }
}