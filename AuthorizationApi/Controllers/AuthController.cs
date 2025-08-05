using AuthorizationApi.DTOs;
using AuthorizationApi.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registration")]
    public async Task<IActionResult> RegisterUser([FromBody] RegistrationRequest request)
    {
        return Ok(await _authService.RegisterUserAsync(request));
    }

    [HttpPost("signing-in")]
    public async Task<IActionResult> SignInUser([FromBody] SigningInRequest request)
    {
        return Ok(await _authService.SingInUserAsync(request));
    }

    [HttpPost("signing-out")]
    public async Task<IActionResult> SignOutUser([FromBody] string token)
    {
        await _authService.SingOutUserAsync(token);
        return NoContent();
    }
}