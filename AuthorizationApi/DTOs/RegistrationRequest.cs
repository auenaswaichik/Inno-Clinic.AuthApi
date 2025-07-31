namespace AuthorizationApi.DTOs;

public record RegistrationRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}