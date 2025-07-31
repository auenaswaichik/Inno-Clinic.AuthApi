namespace AuthorizationApi.DTOs;

public record SigningInRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}