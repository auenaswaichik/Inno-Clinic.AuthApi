using AuthorizationApi.DTOs;
using AuthorizationApi.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorizationController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationController(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    [HttpPost("registration")]
    public async Task<IActionResult> RegisterUser([FromBody] RegistrationRequest request)
    {
        return Ok(await _authorizationService.RegisterUserAsync(request));
    }

    [HttpPost("signing-in")]
    public async Task<IActionResult> SignInUser([FromBody] SigningInRequest request)
    {
        return Ok(await _authorizationService.SingInUserAsync(request));
    }

    [HttpPost("signing-out")]
    public async Task<IActionResult> SignOutUser([FromBody] string token)
    {
        await _authorizationService.SingOutUserAsync(token);
        return NoContent();
    }
}