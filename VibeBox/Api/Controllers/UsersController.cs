using Application.Features.Users.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for managing user-related operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("me")]
    public async Task<ActionResult<GetCurrentUserResponse>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);

        return Ok(response);
    }
}