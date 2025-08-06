using System.Text.Json.Serialization;

namespace AuthApi.Entities;

public class UserResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}