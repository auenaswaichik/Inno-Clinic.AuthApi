namespace AuthApi.Messages.PatientRegisteredMessages;

public sealed class PatientRegisteredMessage
{ 
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string Email { get; set; }
}