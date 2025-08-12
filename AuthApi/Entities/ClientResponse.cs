using System.Text.Json.Serialization;

namespace AuthApi.Entities;

public sealed class ClientResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}