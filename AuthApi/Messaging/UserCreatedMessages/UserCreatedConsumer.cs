using AuthApi.DTOs;
using AuthApi.Interfaces.IServices;
using MassTransit;

namespace AuthApi.Messages.UserCreatedMessages;

public class UserCreatedConsumer : IConsumer<UserCreatedMessage>
{
    private readonly IAuthService _authService;
    public UserCreatedConsumer(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task Consume(ConsumeContext<UserCreatedMessage> context)
    {
        var message = context.Message;

        await _authService.RegisterUserAsync(new RegistrationRequest()
        {
            Username = message.FirstName,
            Email = message.Email,
            Password = "123",
            Role = message.Role
        });

    }
}