using Application.Features.Rooms.Commands.Create;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for managing Room-related operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/rooms")]
public sealed class RoomsController : ControllerBase
{
    private readonly ISender _sender;

    public RoomsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a new room.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ActionResult<CreateRoomResponse>> Create(
        CreateRoomCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            command,
            cancellationToken);

        return Created(
            $"/api/rooms/{response.Id}",
            response);
    }
}