using System.Security.Cryptography.X509Certificates;
using AuthApi.DTOs;
using AuthApi.Entities;

namespace AuthApi.Interfaces.IServices;

public interface IAuthService
{
    public Task<bool> RegisterUserAsync(RegistrationRequest request);
    public Task SingOutUserAsync(string token);
    public string GetAuthorizationRequestUrl();
    public Task<TokenResponse> ExchangeCodeForTokenAsync(string code);
}
