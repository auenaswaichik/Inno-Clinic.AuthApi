using System.Text.Json.Serialization;

namespace AuthApi.Entities;

public sealed class AdminTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}