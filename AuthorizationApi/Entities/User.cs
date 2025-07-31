namespace AuthorizationApi.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string KeycloakId { get; set; }
    public DateTime CreatedAt { get; set; }
}
