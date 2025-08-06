using System.Text.Json.Serialization;

namespace AuthApi.DTOs;

public class ClientResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}