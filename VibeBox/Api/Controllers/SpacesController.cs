using Api.Contracts.Spaces;
using Application.Features.Spaces.Commands.AddSpaceMember;
using Application.Features.Spaces.Commands.CreateSpace;
using Application.Features.Spaces.Commands.RemoveSpaceMember;
using Application.Features.Spaces.Queries.GetMySpaces;
using Application.Features.Spaces.Queries.GetSpaceMembers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for managing spaces and their members. Provides endpoints for creating spaces, retrieving the current user's spaces, and managing space members (adding and removing).
/// </summary>
[ApiController]
[Route("api/spaces")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class SpacesController : ControllerBase
{
    private readonly ISender _sender;

    public SpacesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new space resource and returns the identifier of the created space.
    /// </summary>
    /// <param name="request">The request payload containing the name of the space to create.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An IActionResult with status 201 (Created) and a JSON body containing the created space id. May produce 400 (Bad
    /// Request) or 401 (Unauthorized).</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSpaceRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(new CreateSpaceCommand(request.Name), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>
    /// Gets the spaces for the current user.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An IActionResult with status 200 (OK) and a JSON body containing the list of spaces. May produce 401 (Unauthorized).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SpaceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SpaceResponse>>> GetMySpaces(
        CancellationToken cancellationToken)
    {
        var spaces = await _sender.Send(new GetMySpacesQuery(), cancellationToken);

        var response = spaces
            .Select(space => new SpaceResponse(
                space.Id,
                space.Name,
                space.OwnerId,
                space.CreatedAtUtc))
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Gets the members of a specific space by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the space.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An IActionResult with status 200 (OK) and a JSON body containing the list of space members. May produce 401 (Unauthorized), 403 (Forbidden), or 404 (Not Found).</returns>
    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<SpaceMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SpaceMemberResponse>>> GetMembers(
        Guid id,
        CancellationToken cancellationToken)
    {
        var members = await _sender.Send(new GetSpaceMembersQuery(id), cancellationToken);

        var response = members
            .Select(member => new SpaceMemberResponse(
                member.UserId,
                member.User.Email,
                member.Role.ToString(),
                member.JoinedAtUtc))
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Adds a member to the specified space using the email provided in the request.
    /// </summary>
    /// <remarks>Possible HTTP responses: 400 Bad Request for invalid input; 401 Unauthorized or 403 Forbidden
    /// for authentication/authorization failures; 404 Not Found if the space does not exist; 409 Conflict if the member
    /// already exists.</remarks>
    /// <param name="id">Space identifier (GUID).</param>
    /// <param name="request">Request containing the member's email address.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>NoContent (HTTP 204) on success.</returns>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(
        Guid id,
        [FromBody] AddSpaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AddSpaceMemberCommand(id, request.Email), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Removes a member from the space.
    /// </summary>
    /// <remarks>Produces 401 if unauthorized, 403 if forbidden, 404 if the space or user is not found, and
    /// 409 if the removal conflicts with the current state.</remarks>
    /// <param name="id">The identifier of the space.</param>
    /// <param name="userId">The identifier of the user to remove from the space.</param>
    /// <param name="cancellationToken">A CancellationToken to cancel the operation.</param>
    /// <returns>204 No Content if the member is removed.</returns>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new RemoveSpaceMemberCommand(id, userId),
            cancellationToken);

        return NoContent();
    }
}