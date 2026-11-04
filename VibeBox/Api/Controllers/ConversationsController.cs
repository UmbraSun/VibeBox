using Application.Common.Models;
using Application.Features.Conversations.Commands.CreateDirectConversation;
using Application.Features.Conversations.Queries.GetConversationMessages;
using Application.Features.Conversations.Queries.GetMyConversations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public ConversationsController(ISender sender)
    {
        _sender = sender;
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
}