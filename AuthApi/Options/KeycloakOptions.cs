namespace AuthApi.Options;

public sealed class KeycloakOptions
{
    public string BaseUrl { get; set; }
    public string LocalHostUrl { get; set; }
    public string Realm { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string RedirectUrl { get; set; }
}