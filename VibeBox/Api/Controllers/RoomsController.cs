using Api.Contracts.Rooms;
using Api.Hubs;
using Api.Services;
using Application.Features.Rooms.Commands.AddParticipant;
using Application.Features.Rooms.Commands.Create;
using Application.Features.Rooms.Commands.RemoveParticipant;
using Application.Features.Rooms.Participants;
using Application.Features.Rooms.Queries.GetMyRooms;
using Application.Features.Rooms.Queries.GetParticipants;
using Application.Features.Rooms.Queries.GetRoomById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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
    private readonly IHubContext<CallSignalingHub, ICallSignalingClient> _signalingHub;
    private readonly RoomConnectionTracker _connectionTracker;

    public RoomsController(
        ISender sender, 
        IHubContext<CallSignalingHub, ICallSignalingClient> signalingHub, 
        RoomConnectionTracker connectionTracker)
    {
        _sender = sender;
        _signalingHub = signalingHub;
        _connectionTracker = connectionTracker;
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
        var response = await _sender.Send(command, cancellationToken);

        return Created($"/api/rooms/{response.Id}", response);
    }

    /// <summary>
    /// Retrieves a list of rooms associated with the current user.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MyRoomResponse>>> GetMyRooms(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetMyRoomsQuery(), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves the details of a specific room by its ID.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoomDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetRoomByIdQuery(id), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Adds a participant to a specific room.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/participants")]
    public async Task<ActionResult<RoomParticipantResponse>> AddParticipant(
        Guid id,
        AddRoomParticipantRequest request,
    CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new AddRoomParticipantCommand(id, request.UserId), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Retrieves the list of participants in a specific room.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/participants")]
    public async Task<ActionResult<IReadOnlyList<RoomParticipantResponse>>> GetParticipants(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetRoomParticipantsQuery(id), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Removes a participant from a specific room.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    public async Task<IActionResult> RemoveParticipant(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RemoveRoomParticipantCommand(id, userId), cancellationToken);

        var connectionIds = _connectionTracker.RemoveUserFromRoom(id, userId);
        var groupName = $"room:{id}";

        foreach (var connectionId in connectionIds)
            await _signalingHub.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);

        if (connectionIds.Count > 0)
            await _signalingHub.Clients.Group(groupName)
                .ParticipantLeft(id, userId);

        return NoContent();
    }
}