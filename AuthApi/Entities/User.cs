namespace AuthApi.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string PasswordHash { get; set; }
    public string Email { get; set; }
    public string KeycloakId { get; set; }
    public DateTime CreatedAt { get; set; }
}
