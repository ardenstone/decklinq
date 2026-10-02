using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Trim().Length < 3)
        {
            return BadRequest(new { message = "Username must be at least 3 characters long." });
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters long." });
        }

        var user = _authService.Register(request.Username.Trim(), request.Password);
        if (user is null)
        {
            return Conflict(new { message = "That username is already taken." });
        }

        var token = _authService.CreateToken(user);
        return Ok(new
        {
            token,
            user = new { user.Id, user.Username, user.CreatedAt }
        });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Username and password are required." });
        }

        var user = _authService.Authenticate(request.Username.Trim(), request.Password);
        if (user is null)
        {
            return Unauthorized();
        }

        var token = _authService.CreateToken(user);
        return Ok(new
        {
            token,
            user = new { user.Id, user.Username, user.CreatedAt }
        });
    }

    public record RegisterRequest(string Username, string Password);
    public record LoginRequest(string Username, string Password);
}
