using System.Net.Http.Headers;
using System.Text.Json;
using AuthorizationApi.DTOs;
using AuthorizationApi.Entities;
using AuthorizationApi.Exceptions;
using AuthorizationApi.Interfaces.IRepositories;
using AuthorizationApi.Interfaces.IServices;
using AuthorizationApi.Options;
using Microsoft.Extensions.Options;
using AuthorizationApi.Constants;

namespace AuthorizationApi.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;

    public AuthService(IUserRepository userRepository, IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> keycloakOptions)
    {
        _keycloakOptions = keycloakOptions.Value;
        _httpClientFactory = httpClientFactory;
        _userRepository = userRepository;
    }

    public async Task<bool> RegisterUserAsync(RegistrationRequest request)
    {
        var adminToken = await GetAdminTokenAsync();

        var _httpClient = _httpClientFactory.CreateClient("KeycloakClient");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthServiceConstants.AUTHORIZATION_HEADER, adminToken);

        var userPayload = new
        {
            username = request.Username,
            email = request.Email,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = AuthServiceConstants.USER_CREDENTIAL_PASSWORD_TYPE, value = request.Password, temporary = false }
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

        var clientsResponse = await _httpClient.GetAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/clients?clientId={_keycloakOptions.ClientId}"
        );

        if (!clientsResponse.IsSuccessStatusCode)
        {
            throw new BadRequestException($"Failed to get clients");
        }

        var clientContetn = JsonSerializer.Deserialize<List<ClientResponse>>(await clientsResponse.Content.ReadAsStringAsync());

        var internalClientId = clientContetn.FirstOrDefault()?.Id;

        var rolesResponse = await _httpClient.GetAsync(
            $"/admin/realms/{_keycloakOptions.Realm}/clients/{internalClientId}/roles/{request.Role}"
        );

        if (!rolesResponse.IsSuccessStatusCode)
        {
            throw new NotFoundException($"Failed to get role '{request.Role}': {rolesResponse.StatusCode}\n");
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
            throw new BadRequestException($"Failed to assign role '{request.Role}' to user: {assignRoleResponse.StatusCode}\n");
        }

        // _userRepository.Insert(new User
        // {
        //     Name = request.Username,
        //     Email = request.Email,
        //     KeycloakId = adminToken,
        //     CreatedAt = DateTime.UtcNow
        // });
        // await _userRepository.SaveChangesAsync();

        return true;
    }

    public async Task<TokenResponse> SingInUserAsync(SigningInRequest request)
    {
        var _httpClient = _httpClientFactory.CreateClient("KeycloakClient");

        var data = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", _keycloakOptions.ClientId },
            { "client_secret", _keycloakOptions.ClientSecret },
            { "username", request.Username },
            { "password", request.Password }
        };

        var response = await _httpClient.PostAsync($"/realms/{_keycloakOptions.Realm}/protocol/openid-connect/token", new FormUrlEncodedContent(data));

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var content = await response.Content.ReadAsStringAsync();
       
        return JsonSerializer.Deserialize<TokenResponse>(content);
    }

    public async Task SingOutUserAsync(string refreshToken)
    {
        var _httpClient = _httpClientFactory.CreateClient("KeycloakClient");

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
        var _httpClient = _httpClientFactory.CreateClient("KeycloakClient");

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

        var adminTokenContent = JsonSerializer.Deserialize<List<AdminTokenResponse>>(await response.Content.ReadAsStringAsync());

        var adminToken = adminTokenContent.FirstOrDefault()?.AccessToken;

        return adminToken;
    }
}