using System.Text.Json.Serialization;

namespace AuthorizationApi.Entities;

public class UserResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}