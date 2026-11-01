using Api.Contracts.Rooms;
using Application.Features.Channels.Commands.CreateLiveKitToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/channels")]
public sealed class ChannelsController : ControllerBase
{
    private readonly ISender _sender;

    public ChannelsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Creates a LiveKit access token for an authorized voice channel member.
    /// </summary>
    [HttpPost("{channelId:guid}/livekit-token")]
    [ProducesResponseType(typeof(LiveKitTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LiveKitTokenResponse>> CreateLiveKitToken(
        Guid channelId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateLiveKitTokenCommand(channelId),
            cancellationToken);

        return Ok(new LiveKitTokenResponse(
            result.ServerUrl,
            result.ParticipantToken,
            result.RoomName));
    }
}