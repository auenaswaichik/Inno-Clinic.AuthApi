using AuthApi.Enums;

namespace AuthApi.Messages.UserCreatedMessages;

public sealed class UserCreatedMessage
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string Email { get; set; }
    public Roles Role { get; set; }
    public DateTime CreatedAt { get; set; }
}