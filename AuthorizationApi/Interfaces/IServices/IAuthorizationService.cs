using System.Security.Cryptography.X509Certificates;
using AuthorizationApi.DTOs;

namespace AuthorizationApi.Interfaces.IServices;

public interface IAuthorizationService
{
    public Task<bool> RegisterUserAsync(RegistrationRequest request);
    public Task<TokenResponse> SingInUserAsync(SigningInRequest request);
    public Task SingOutUserAsync(string token);
}
