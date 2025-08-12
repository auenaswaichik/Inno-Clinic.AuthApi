using System.Text.Json.Serialization;

namespace AuthApi.Entities;

public sealed class UserResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}