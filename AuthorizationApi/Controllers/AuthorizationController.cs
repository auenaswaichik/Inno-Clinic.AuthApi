using AuthorizationApi.DTOs;
using AuthorizationApi.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : ControllerBase
{
    private readonly IKeycloakService _keycloakService;

    public AuthorizationController(IKeycloakService keycloakService)
    {
        _keycloakService = keycloakService;
    }

    [HttpPost("registration")]
    public async Task<IActionResult> RegisterUser([FromBody] RegistrationRequest request)
    {
        return Ok(await _keycloakService.RegisterUserAsync(request));
    }

    [HttpPost("signing-in")]
    public async Task<IActionResult> SignInUser([FromBody] SigningInRequest request)
    {
        return Ok(await _keycloakService.SingInUserAsync(request));
    }

    [HttpPost("signing-out")]
    public async Task<IActionResult> SignOutUser([FromBody] string token)
    {
        await _keycloakService.SingOutUserAsync(token);
        return NoContent();
    }
}