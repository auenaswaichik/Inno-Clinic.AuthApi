using System.Security.Cryptography.X509Certificates;
using AuthorizationApi.DTOs;

namespace AuthorizationApi.Interfaces.IServices;

public interface IKeycloakService
{
    public Task<bool> RegisterUserAsync(RegistrationRequest request);
    public Task<TokenResponse> SingInUserAsync(SingingInRequest request);
    public Task SingOutUserAsync(string token);
}
