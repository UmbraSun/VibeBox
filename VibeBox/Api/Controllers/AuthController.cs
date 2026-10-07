using Application.Features.Users.Commands.Login;
using Application.Features.Users.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for handling user authentication, including registration and login.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers a new user with the provided email and password.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await _sender.Send(command, cancellationToken);

        return Created($"/api/users/{userId}", new { userId });
    }

    /// <summary>
    /// Authenticates a user and returns an access token and refresh token upon successful login.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }
}