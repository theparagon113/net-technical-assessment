using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.SharedApi;
using TaskManager.Application.Authentication;

namespace TaskManager.Auth.Api.Authentication;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register(AuthInput input, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(input, cancellationToken));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login(AuthInput input, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(input, cancellationToken));

    [AllowAnonymous]
    [HttpGet("public")]
    public IActionResult Public() => Ok(new { Message = "Task Manager authentication API" });

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new { UserId = CurrentIdentity.UserId(User), Username = User.Identity!.Name });
}

