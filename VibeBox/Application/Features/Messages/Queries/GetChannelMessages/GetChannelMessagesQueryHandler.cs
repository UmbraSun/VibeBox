using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;

namespace Application.Features.Messages.Queries.GetChannelMessages;

public sealed class GetChannelMessagesQueryHandler
    : IRequestHandler<GetChannelMessagesQuery, IReadOnlyList<MessageResponse>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetChannelMessagesQueryHandler(
        IMessageRepository messageRepository,
        IMessageReactionRepository reactionRepository,
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<MessageResponse>> Handle(
        GetChannelMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Limit is < 1 or > 100)
        {
            throw new FluentValidation.ValidationException(
                "Limit must be between 1 and 100.");
        }

        var currentUserId = _currentUserService.UserId;

        var channel = await _spaceRepository.GetChannelByIdAsync(
            request.ChannelId,
            cancellationToken);

        if (channel is null)
            throw new KeyNotFoundException("Channel was not found.");

        if (channel.Type != ChannelType.Text)
        {
            throw new ConflictException(
                "Messages can only be retrieved from text channels.");
        }

        var member = await _spaceRepository.GetMemberAsync(
            channel.SpaceId,
            currentUserId,
            cancellationToken);

        if (member is null)
        {
            throw new ForbiddenAccessException(
                "You are not a member of this space.");
        }

        var messages = await _messageRepository.GetByChannelAsync(
            channel.Id,
            request.BeforeMessageId,
            request.Limit,
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
            message.Attachments.Select(attachment =>
                new MessageAttachmentResponse(
                    attachment.Id,
                    attachment.FileName,
                    attachment.ContentType,
                    attachment.SizeBytes))
                .ToList(),
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