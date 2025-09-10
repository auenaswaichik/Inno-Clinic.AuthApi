using System.Net.Http.Headers;
using System.Text.Json;
using AuthApi.DTOs;
using AuthApi.Entities;
using AuthApi.Exceptions;
using AuthApi.Interfaces.IRepositories;
using AuthApi.Interfaces.IServices;
using AuthApi.Options;
using Microsoft.Extensions.Options;
using AuthApi.Constants;
using MassTransit;
using AuthApi.Messages.PatientRegisteredMessages;
using System.Security.Cryptography;
using System.Text;
using AuthApi.Enums;

namespace AuthApi.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuthService(IUserRepository userRepository, IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> keycloakOptions, IPublishEndpoint publishEndpoint)
    {
        _keycloakOptions = keycloakOptions.Value;
        _httpClientFactory = httpClientFactory;
        _userRepository = userRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> RegisterUserAsync(RegistrationRequest request)
    {
        if (request.Role != Roles.Patient)
            throw new BadRequestException("Only 'Patient' role can be registered through this endpoint.");

        var adminToken = await GetAdminTokenAsync();

        using var _httpClient = _httpClientFactory.CreateClient(AuthConstants.KEYCLOAK_CLIENT);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthConstants.AUTHORIZATION_HEADER, adminToken);

        var userPayload = new
        {
            username = request.Username,
            email = request.Email,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = AuthConstants.USER_CREDENTIAL_PASSWORD_TYPE, value = request.Password, temporary = false }
            }
        };

        var createUserResponse = await _httpClient.PostAsJsonAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/users",
            userPayload
        );

        if (!createUserResponse.IsSuccessStatusCode)
        {
            throw new BadRequestException(createUserResponse.StatusCode.ToString());
        }

        var userResponse = await _httpClient.GetAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/users?username={request.Username}"
        );

        var userContent = JsonSerializer.Deserialize<List<UserResponse>>(await userResponse.Content.ReadAsStringAsync());

        var userId = userContent.FirstOrDefault()?.Id;
        
        if (string.IsNullOrEmpty(userId))
        {
            throw new BadRequestException("Failed to retrieve user ID after creation");
        }

        var clientsResponse = await _httpClient.GetAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/clients?clientId={_keycloakOptions.ClientId}"
        );

        if (!clientsResponse.IsSuccessStatusCode)
        {
            throw new BadRequestException($"Failed to get clients");
        }

        var clientContetn = JsonSerializer.Deserialize<List<ClientResponse>>(await clientsResponse.Content.ReadAsStringAsync());

        var internalClientId = clientContetn.FirstOrDefault()?.Id;
        
        if (string.IsNullOrEmpty(internalClientId))
        {
            throw new BadRequestException($"Failed to retrieve client ID for client: {_keycloakOptions.ClientId}");
        }

        var rolesResponse = await _httpClient.GetAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/clients/{internalClientId}/roles/{request.Role.ToString()}"
        );

        if (!rolesResponse.IsSuccessStatusCode)
        {
            throw new NotFoundException($"Failed to get role '{request.Role.ToString()}': {rolesResponse.StatusCode}\n");
        }

        var roleJson = await rolesResponse.Content.ReadAsStringAsync();
        
        var roleDoc = JsonDocument.Parse(roleJson).RootElement;

        var roleToAssign = new[]
        {
            new
            {
                id = roleDoc.GetProperty("id").GetString(),
                name = roleDoc.GetProperty("name").GetString()
            }
        };

        var assignRoleResponse = await _httpClient.PostAsJsonAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/users/{userId}/role-mappings/clients/{internalClientId}",
            roleToAssign
        );

        if (!assignRoleResponse.IsSuccessStatusCode)
        {
            var errorContent = await assignRoleResponse.Content.ReadAsStringAsync();
            throw new BadRequestException($"Failed to assign role '{request.Role.ToString()}' to user: {assignRoleResponse.StatusCode}. Error: {errorContent}\n");
        }

        var user = new User
        {
            Login = request.Username,
            PasswordHash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(request.Password)).ToString(),
            Email = request.Email,
            KeycloakId = adminToken,
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.Insert(user);
        
        await _publishEndpoint.Publish(new PatientRegisteredMessage()
        {
            Id = user.Id,
            Login = user.Login,
            Email = user.Email
        });
            
        await _userRepository.SaveChangesAsync();

        return true;
    }

    public string GetAuthorizationRequestUrl()
    {
        var redirectUri = Uri.EscapeDataString(_keycloakOptions.RedirectUrl);
        var authUrl = $"{_keycloakOptions.LocalHostUrl}/realms/{_keycloakOptions.Realm}/protocol/openid-connect/auth" +
                    $"?client_id={_keycloakOptions.ClientId}" +
                    $"&response_type=code" +
                    $"&scope=openid profile email roles" +
                    $"&redirect_uri={redirectUri}";

        return authUrl;
    }

    public async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
    {
        using var client = _httpClientFactory.CreateClient(AuthConstants.KEYCLOAK_CLIENT);

        var data = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", _keycloakOptions.RedirectUrl },
            { "client_id", _keycloakOptions.ClientId },
            { "client_secret", _keycloakOptions.ClientSecret },
            { "scope", "openid profile email roles" }
        };

        var response = await client.PostAsync(
            $"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(data));

        if (!response.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Code exchange failed");

        var json = await response.Content.ReadAsStringAsync();

        Console.WriteLine(json);

        var a = 1;
        return JsonSerializer.Deserialize<TokenResponse>(json);
    }


    public async Task SingOutUserAsync(string refreshToken)
    {
        using var _httpClient = _httpClientFactory.CreateClient(AuthConstants.KEYCLOAK_CLIENT);

        var data = new Dictionary<string, string>
        {
            { "client_id", $"{_keycloakOptions.ClientId}" },
            { "client_secret", $"{_keycloakOptions.ClientSecret}" },
            { "refresh_token", refreshToken }
        };

        await _httpClient.PostAsync($"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/logout", new FormUrlEncodedContent(data));
    }

    private async Task<string> GetAdminTokenAsync()
    {
        using var _httpClient = _httpClientFactory.CreateClient(AuthConstants.KEYCLOAK_CLIENT);

        var data = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", $"{_keycloakOptions.ClientId}" },
            { "client_secret", $"{_keycloakOptions.ClientSecret}" }
        };

        var response = await _httpClient.PostAsync(
            $"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/token",
            new FormUrlEncodedContent(data)
        );

        var adminTokenContent = JsonSerializer.Deserialize<AdminTokenResponse>(await response.Content.ReadAsStringAsync());

        var adminToken = adminTokenContent.AccessToken;

        return adminToken;
    }
}