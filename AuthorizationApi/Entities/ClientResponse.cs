using System.Text.Json.Serialization;

namespace AuthorizationApi.DTOs;

public class ClientResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}