using Api.Contracts.Messages;
using Application.Features.Messages.Commands.SendMessage;
using Application.Features.Messages.Queries.GetChannelMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controller for handling message-related operations, including sending messages and retrieving message history for a specific channel.
/// </summary>
[ApiController]
[Route("api/channels/{channelId:guid}/messages")]
[Authorize]
public sealed class MessagesController : ControllerBase
{
    private readonly ISender _sender;

    public MessagesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Sends a message to the specified channel. The request body must contain the message content. Returns the ID of the newly created message upon success.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Send(
        Guid channelId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new SendMessageCommand(channelId, request.Content),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>
    /// Retrieves the message history for the specified channel. Supports optional query parameters to filter messages before a specific message ID and limit the number of messages returned. Returns a list of messages in the response.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="beforeMessageId"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MessageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MessageResponse>>> GetHistory(
        Guid channelId,
        [FromQuery] Guid? beforeMessageId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await _sender.Send(
            new GetChannelMessagesQuery(channelId, beforeMessageId, limit),
            cancellationToken);

        var response = messages.Select(message => new MessageResponse(
            message.Id,
            message.ChannelId,
            message.SenderId,
            message.Sender.Email,
            message.Content,
            message.CreatedAtUtc,
            message.UpdatedAtUtc)).ToList();

        return Ok(response);
    }
}