using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;

    public UsersController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var user = _authService.GetCurrentUser(User);
        return user is null ? Unauthorized() : Ok(new { user.Id, user.Username, user.CreatedAt });
    }
}
