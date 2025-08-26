using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthApi.Constants;
using AuthApi.DTOs;
using AuthApi.Entities;
using AuthApi.Exceptions;
using AuthApi.Interfaces.IRepositories;
using AuthApi.Options;
using MassTransit;
using Microsoft.Extensions.Options;

namespace AuthApi.Messages.UserCreatedMessages;

public sealed class UserCreatedConsumer : IConsumer<UserCreatedMessage>
{
    private readonly IUserRepository _userRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly KeycloakOptions _keycloakOptions;
    public UserCreatedConsumer(IUserRepository userRepository, IHttpClientFactory httpClientFactory, IOptions<KeycloakOptions> keycloakOptions)
    {
        _keycloakOptions = keycloakOptions.Value;
        _httpClientFactory = httpClientFactory;
        _userRepository = userRepository;
    }

    public async Task Consume(ConsumeContext<UserCreatedMessage> context)
    {
        var message = context.Message;
        var adminToken = await GetAdminTokenAsync();

        using var _httpClient = _httpClientFactory.CreateClient(AuthConstants.KEYCLOAK_CLIENT);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthConstants.AUTHORIZATION_HEADER, adminToken);

        var tempPassword = GetRandomPassword();

        var userPayload = new
        {
            username = message.FirstName,
            email = message.Email,
            enabled = true,
            emailVerified = true,
            credentials = new[]
            {
                new { type = AuthConstants.USER_CREDENTIAL_PASSWORD_TYPE, value = tempPassword, temporary = true }
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
            $"/admin/realms/{_keycloakOptions.Realm}/users?username={message.FirstName}"
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
            $"/admin/realms/{_keycloakOptions.Realm}/clients/{internalClientId}/roles/{message.Role}"
        );

        if (!rolesResponse.IsSuccessStatusCode)
        {
            throw new NotFoundException($"Failed to get role '{message.Role}': {rolesResponse.StatusCode}\n");
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
            throw new BadRequestException($"Failed to assign role '{message.Role}' to user: {assignRoleResponse.StatusCode}\n");
        }

        var user = new User
        {
            Id = message.Id,
            Login = message.FirstName,
            PasswordHash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(tempPassword)).ToString(),
            Email = message.Email,
            KeycloakId = adminToken,
            CreatedAt = DateTime.UtcNow
        };

        _userRepository.Insert(user);

        await _userRepository.SaveChangesAsync();
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
    
    private string GetRandomPassword(int length = 8)
    {
        const string VALID_CHARS = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()?_-";
        var random = new Random();
        return new string(Enumerable.Repeat(VALID_CHARS, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}