using System.Text.Json.Serialization;

namespace AuthorizationApi.Entities;

public class AdminTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}