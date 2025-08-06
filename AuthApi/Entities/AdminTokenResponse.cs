using System.Text.Json.Serialization;

namespace AuthApi.Entities;

public class AdminTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}