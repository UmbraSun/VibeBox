using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;

namespace Application.Features.Conversations.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandler
    : IRequestHandler<GetConversationMessagesQuery, IReadOnlyList<MessageResponse>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationMessagesQueryHandler(
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IMessageReactionRepository reactionRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<MessageResponse>> Handle(
        GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var conversation = await _conversationRepository.GetByIdForMemberAsync(
            request.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            throw new KeyNotFoundException("Conversation not found.");

        var limit = Math.Clamp(request.Limit, 1, 100);

        var messages = await _messageRepository.GetByConversationAsync(
            request.ConversationId,
            request.BeforeMessageId,
            limit,
            cancellationToken);

        if (messages.Count == 0)
            return Array.Empty<MessageResponse>();

        var messageIds = messages
            .Select(message => message.Id)
            .ToArray();

        var reactions = await _reactionRepository.GetByMessageIdsAsync(
            messageIds,
            cancellationToken);

        var reactionsByMessage = reactions
            .GroupBy(reaction => reaction.MessageId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MessageReactionResponse>)group
                    .GroupBy(reaction => reaction.Emoji)
                    .Select(emojiGroup => new MessageReactionResponse(
                        emojiGroup.Key,
                        emojiGroup.Count(),
                        emojiGroup.Any(reaction =>
                            reaction.UserId == currentUserId)))
                    .ToList());

        return messages.Select(message => new MessageResponse(
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
                attachment.SizeBytes
            )).ToList(),
            message.ReplyToMessageId,
            message.ReplyToMessage is null
                ? null
                : new MessageReplyResponse(
                    message.ReplyToMessage.Id,
                    message.ReplyToMessage.SenderId,
                    message.ReplyToMessage.Sender.Email,
                    message.ReplyToMessage.Content),
            reactionsByMessage.TryGetValue(message.Id, out var messageReactions)
                ? messageReactions
                : Array.Empty<MessageReactionResponse>()
        )).ToList();
    }
}