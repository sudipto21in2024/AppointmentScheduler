using Microsoft.AspNetCore.Mvc;
using UserService.Services;

using Microsoft.AspNetCore.Mvc;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public IActionResult Login(string email, string password)
    {
        var token = _authService.Login(email, password);
        return Ok(token);
    }

    [HttpPost("register")]
    public IActionResult Register(string email, string password, string role)
    {
        //TODO: Implement role parsing
        _authService.Register(email, password, Shared.Models.UserRole.Customer);
        return Ok();
    }

    [HttpPost("logout")]
    public IActionResult Logout(string token)
    {
        _authService.Logout(token);
        return Ok();
    }

    [HttpPost("refresh")]
    public IActionResult Refresh(string refreshToken)
    {
        var token = _authService.Refresh(refreshToken);
        return Ok(token);
    }

    [HttpPost("request-password-reset")]
    public IActionResult RequestPasswordReset(string email)
    {
        _authService.RequestPasswordReset(email);
        return Ok();
    }

    [HttpPost("reset-password")]
    public IActionResult ResetPassword(string email, string token, string newPassword)
    {
        _authService.ResetPassword(email, token, newPassword);
        return Ok();
    }
}