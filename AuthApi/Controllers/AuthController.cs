using AuthApi.DTOs;
using AuthApi.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

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

    [HttpGet("signin")]
    public IActionResult SignIn()
    {
        var url = _authService.GetAuthorizationRequestUrl();
        return Redirect(url); 
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        var tokens = await _authService.ExchangeCodeForTokenAsync(code);
        return Ok(tokens);
    }

    [HttpPost("signout")]
    public async Task<IActionResult> SignOutUser([FromBody] string token)
    {
        await _authService.SingOutUserAsync(token);
        return NoContent();
    }
}