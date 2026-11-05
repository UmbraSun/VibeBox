using Application.Features.Users.Queries.GetCurrentUser;
using Application.Features.Users.Queries.SearchUsers;
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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current user's information.</returns>
    [HttpGet("me")]
    public async Task<ActionResult<GetCurrentUserResponse>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Searches for users by email. Returns a list of users matching the provided email query.
    /// </summary>
    /// <param name="email">The email to search for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of users matching the email query.</returns>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<SearchUsersResponse>>> Search(
        [FromQuery] string email,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new SearchUsersQuery(email),
            cancellationToken);

        return Ok(response);
    }
}