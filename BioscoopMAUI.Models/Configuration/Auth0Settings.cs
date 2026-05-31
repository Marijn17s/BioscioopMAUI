namespace BioscoopMAUI.Models.Configuration;

public class Auth0Settings
{
    public const string SectionName = "Auth0";

    public string Domain { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string PostLogoutRedirectUri { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;
}