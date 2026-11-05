using Api.Contracts.Conversations;
using Api.Hubs;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Features.Conversations.Commands.CreateDirectConversation;
using Application.Features.Conversations.Commands.SendConversationMessage;
using Application.Features.Conversations.Queries.GetConversationMessages;
using Application.Features.Conversations.Queries.GetMyConversations;
using Application.Features.Messages.Commands.DeleteMessage;
using Application.Features.Messages.Commands.UploadMessageAttachment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Api.Controllers;

/// <summary>
/// Controller for managing conversations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IHubContext<MessageHub> _messageHub;
    private readonly IConversationRepository _conversationRepository;

    public ConversationsController(
        ISender sender,
        IHubContext<MessageHub> messageHub,
        IConversationRepository conversationRepository)
    {
        _sender = sender;
        _messageHub = messageHub;
        _conversationRepository = conversationRepository;
    }

    /// <summary>
    /// Creates a direct conversation between the current user and the specified user.
    /// </summary>
    /// <param name="request">The request containing the target user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the created conversation.</returns>
    [HttpPost("direct")]
    public async Task<IActionResult> CreateDirect(
        [FromBody] CreateDirectConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversationId = await _sender.Send(
            new CreateDirectConversationCommand(request.UserId),
            cancellationToken);

        return Ok(new { id = conversationId });
    }

    /// <summary>
    /// Gets the list of conversations for the current user.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of conversations.</returns>
    [HttpGet]
    public async Task<IActionResult> GetMyConversations(
        CancellationToken cancellationToken)
    {
        var conversations = await _sender.Send(
            new GetMyConversationsQuery(),
            cancellationToken);

        return Ok(conversations);
    }

    /// <summary>
    /// Gets the messages for a specific conversation, optionally filtering by a message ID and limiting the number of messages returned.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation for which to retrieve messages.</param>
    /// <param name="beforeMessageId">The ID of the message before which to filter results.</param>
    /// <param name="limit">The maximum number of messages to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of messages.</returns>
    [HttpGet("{conversationId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid conversationId,
        [FromQuery] Guid? beforeMessageId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var messages = await _sender.Send(
            new GetConversationMessagesQuery(
                conversationId,
                beforeMessageId,
                limit),
            cancellationToken);

        return Ok(messages);
    }

    /// <summary>
    /// Sends a message in a specific conversation.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation in which to send the message.</param>
    /// <param name="request">The request containing the message content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the sent message.</returns>
    [HttpPost("{conversationId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendConversationMessageRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(
            new SendConversationMessageCommand(
                conversationId,
                request.Content),
            cancellationToken);

        await _messageHub.Clients
            .Group(MessageHub.GetConversationGroupName(conversationId))
            .SendAsync(
                "MessageCreated",
                new { Id = id, ConversationId = conversationId },
                cancellationToken);

        await NotifyConversationUpdatedAsync(
            conversationId,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new { id });
    }

    /// <summary>
    /// Uploads one or more attachments to a specific conversation. The request must be a multipart/form-data request containing the files and optional content. Returns the ID of the newly created message with attachments upon success.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to which to upload attachments.</param>
    /// <param name="files">The files to upload as attachments.</param>
    /// <param name="content">The content of the message (optional).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ID of the newly created message with attachments.</returns>
    [HttpPost("{conversationId:guid}/messages/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(160 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 160 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadMessageAttachments(
        Guid conversationId,
        [FromForm] List<IFormFile> files,
        [FromForm] string? content,
        CancellationToken cancellationToken)
    {
        var uploads = new List<UploadMessageAttachmentFile>(files.Count);
        var streams = new List<Stream>(files.Count);

        try
        {
            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);

                uploads.Add(new UploadMessageAttachmentFile(
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream));
            }

            var id = await _sender.Send(
                new UploadMessageAttachmentCommand(
                    null,
                    conversationId,
                    content,
                    uploads),
                cancellationToken);

            await _messageHub.Clients
                .Group(MessageHub.GetConversationGroupName(conversationId))
                .SendAsync(
                    "MessageCreated",
                    new { Id = id, ConversationId = conversationId },
                    cancellationToken);

            await NotifyConversationUpdatedAsync(
                conversationId,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new { id });
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }
    }

    /// <summary>
    /// Deletes a specific message from a conversation. The message will be removed for all participants in the conversation. Returns a 204 No Content response upon successful deletion.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation from which to delete the message.</param>
    /// <param name="messageId">The ID of the message to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    [HttpDelete("{conversationId:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(
        Guid conversationId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new DeleteMessageCommand(null, conversationId, messageId),
            cancellationToken);

        await _messageHub.Clients
            .Group(MessageHub.GetConversationGroupName(conversationId))
            .SendAsync(
                "MessageDeleted",
                new
                {
                    Id = messageId,
                    ConversationId = conversationId
                },
                cancellationToken);

        await NotifyConversationUpdatedAsync(
            conversationId,
            cancellationToken);

        return NoContent();
    }

    private async Task NotifyConversationUpdatedAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var memberIds = await _conversationRepository.GetMemberIdsAsync(
            conversationId,
            cancellationToken);

        var groups = memberIds
            .Select(MessageHub.GetUserGroupName)
            .ToList();

        if (groups.Count == 0)
            return;

        await _messageHub.Clients
            .Groups(groups)
            .SendAsync(
                "ConversationUpdated",
                new { ConversationId = conversationId },
                cancellationToken);
    }
}