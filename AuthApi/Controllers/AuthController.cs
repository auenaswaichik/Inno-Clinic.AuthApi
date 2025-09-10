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

        if (!string.IsNullOrWhiteSpace(tokens?.AccessToken))
        {
            Response.Cookies.Append("access_token", tokens.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn > 0 ? tokens.ExpiresIn : 3600)
            });
            Response.Cookies.Append("id_token", tokens.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn > 0 ? tokens.ExpiresIn : 3600)
            });
        }

        return Redirect("http://localhost:3000");
    }

    [HttpPost("signout")]
    public async Task<IActionResult> SignOutUser([FromBody] string token)
    {
        await _authService.SingOutUserAsync(token);
        return NoContent();
    }
    [HttpGet("userinfo")]
    public async Task<IActionResult> GetUserInfo()
    {
        return Ok(new {cookies = Request.Cookies.TryGetValue("id_token", out var id_token) ? id_token : "No id token cookie found"});
    }
}