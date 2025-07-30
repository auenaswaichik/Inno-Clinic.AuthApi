namespace Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string KeycloakId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User(Guid id, string name, string email, string keycloakId)
    {
        Id = id;
        Name = name;
        Email = email;
        KeycloakId = keycloakId;
        CreatedAt = DateTime.UtcNow;
    }
}
