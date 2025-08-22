namespace AuthApi.Messages.UserCreatedMessages;

public sealed class UserCreatedMessage
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
}