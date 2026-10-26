using Api.Contracts.Messages;
using Api.Hubs;
using Application.Features.Messages.Commands.SendMessage;
using Application.Features.Messages.Commands.UploadMessageAttachment;
using Application.Features.Messages.Queries.GetChannelMessages;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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

    private readonly IHubContext<MessageHub> _messageHub;

    public MessagesController(
        ISender sender,
        IHubContext<MessageHub> messageHub)
    {
        _sender = sender;
        _messageHub = messageHub;
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

        await _messageHub.Clients
            .Group(MessageHub.GetChannelGroupName(channelId))
            .SendAsync("MessageCreated", new { Id = id, ChannelId = channelId }, cancellationToken);

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
            message.UpdatedAtUtc,
            message.Attachments.Select(attachment => new MessageAttachmentResponse(
                attachment.Id,
                attachment.FileName,
                attachment.ContentType,
                attachment.SizeBytes)).ToList()
        )).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Uploads an attachment to the specified channel.
    /// </summary>
    /// <param name="channelId">The unique identifier of the channel.</param>
    /// <param name="file">The file to upload as an attachment.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A result indicating the outcome of the upload operation.</returns>
    [HttpPost("attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(110 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 110 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UploadAttachment(
        Guid channelId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "A non-empty file is required." });

        await using var stream = file.OpenReadStream();

        var id = await _sender.Send(
            new UploadMessageAttachmentCommand(
                channelId,
                file.FileName,
                file.ContentType,
                file.Length,
                stream),
            cancellationToken);

        await _messageHub.Clients
            .Group(MessageHub.GetChannelGroupName(channelId))
            .SendAsync("MessageCreated", new { Id = id, ChannelId = channelId }, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { id });
    }
}