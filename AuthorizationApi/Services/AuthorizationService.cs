using System.Net.Http.Headers;
using System.Text.Json;
using AuthorizationApi.DTOs;
using AuthorizationApi.Entities;
using AuthorizationApi.Exceptions;
using AuthorizationApi.Interfaces.IRepositories;
using AuthorizationApi.Interfaces.IServices;

namespace AuthorizationApi.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IUserRepository _userRepository;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public AuthorizationService(IUserRepository userRepository, HttpClient httpClient, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<bool> RegisterUserAsync(RegistrationRequest request)
    {
        var keycloakBaseUrl = _configuration["Keycloak:BaseUrl"];
        var realm = _configuration["Keycloak:Realm"];

        var adminToken = await GetAdminTokenAsync();

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var userPayload = new
        {
            username = request.Username,
            email = request.Email,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = "password", value = request.Password, temporary = false }
            }
        };

        var createUserResponse = await _httpClient.PostAsJsonAsync($"http://{keycloakBaseUrl}/admin/realms/{realm}/users", userPayload);
        if (!createUserResponse.IsSuccessStatusCode)
        {
            throw new BadRequestException(createUserResponse.StatusCode.ToString());
        }

        var getUserResponse = await _httpClient.GetAsync($"http://{keycloakBaseUrl}/admin/realms/{realm}/users?username={request.Username}");
        var usersJson = await getUserResponse.Content.ReadAsStringAsync();
        var userId = JsonDocument.Parse(usersJson).RootElement[0].GetProperty("id").GetString();

        var clientId = _configuration["Keycloak:ClientId"];
        var getClientsResponse = await _httpClient.GetAsync(
            $"http://{keycloakBaseUrl}/admin/realms/{realm}/clients?clientId={clientId}"
        );
        var clientsJson = await getClientsResponse.Content.ReadAsStringAsync();

        if (!getClientsResponse.IsSuccessStatusCode)
            throw new Exception($"Failed to get clients: {clientsJson}");

        var clientsArray = JsonDocument.Parse(clientsJson).RootElement;
        var client = clientsArray.EnumerateArray().FirstOrDefault();
        var internalClientId = client.GetProperty("id").GetString(); 

        var getRolesResponse = await _httpClient.GetAsync($"http://{keycloakBaseUrl}/admin/realms/{realm}/clients/{internalClientId}/roles/{request.Role}");

        if (!getRolesResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get role '{request.Role}': {getRolesResponse.StatusCode}\n");
        }

        var roleJson = await getRolesResponse.Content.ReadAsStringAsync();
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
            $"http://{keycloakBaseUrl}/admin/realms/{realm}/users/{userId}/role-mappings/clients/{internalClientId}",
            roleToAssign
        );

        if (!assignRoleResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to assign role '{request.Role}' to user: {assignRoleResponse.StatusCode}\n");
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
        var data = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", _configuration["Keycloak:ClientId"] },
            { "client_secret", _configuration["Keycloak:ClientSecret"] },
            { "username", request.Username },
            { "password", request.Password }
        };

        var response = await _httpClient.PostAsync($"http://{_configuration["Keycloak:BaseUrl"]}/realms/{_configuration["Keycloak:Realm"]}/protocol/openid-connect/token", new FormUrlEncodedContent(data));

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TokenResponse>(content);
    }

    public async Task SingOutUserAsync(string refreshToken)
    {
        var data = new Dictionary<string, string>
        {
            { "client_id", $"{_configuration["Keycloak:ClientId"]}" },
            { "client_secret", $"{_configuration["Keycloak:ClientSecret"]}" },
            { "refresh_token", refreshToken }
        };

        await _httpClient.PostAsync($"http://{_configuration["Keycloak:BaseUrl"]}/realms/{_configuration["Keycloak:Realm"]}/protocol/openid-connect/logout", new FormUrlEncodedContent(data));
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var data = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", $"{_configuration["Keycloak:ClientId"]}" },
            { "client_secret", $"{_configuration["Keycloak:ClientSecret"]}" }
        };

        var response = await _httpClient.PostAsync($"http://{_configuration["Keycloak:BaseUrl"]}/realms/{_configuration["Keycloak:Realm"]}/protocol/openid-connect/token", new FormUrlEncodedContent(data));
        var content = await response.Content.ReadAsStringAsync();
        var token = JsonDocument.Parse(content).RootElement.GetProperty("access_token").GetString();

        return token;
    }
}