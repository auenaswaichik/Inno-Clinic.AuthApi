namespace AuthorizationApi.DTOs;

public record SingingInRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}