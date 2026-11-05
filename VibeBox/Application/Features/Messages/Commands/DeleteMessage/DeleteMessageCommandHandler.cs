using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.Messages.Commands.DeleteMessage;

public sealed class DeleteMessageCommandHandler
    : IRequestHandler<DeleteMessageCommand>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public DeleteMessageCommandHandler(
        IMessageRepository messageRepository,
        ISpaceRepository spaceRepository,
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext context)
    {
        _messageRepository = messageRepository;
        _spaceRepository = spaceRepository;
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task Handle(
        DeleteMessageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ChannelId.HasValue == request.ConversationId.HasValue)
        {
            throw new ValidationException(
                "Exactly one of ChannelId or ConversationId must be specified.");
        }

        var currentUserId = _currentUserService.UserId;

        var message = await _messageRepository.GetByIdAsync(
            request.MessageId,
            cancellationToken);

        if (message is null ||
            message.ChannelId != request.ChannelId ||
            message.ConversationId != request.ConversationId)
        {
            throw new KeyNotFoundException("Message was not found.");
        }

        if (message.SenderId != currentUserId)
        {
            throw new ForbiddenAccessException(
                "You can only delete your own messages.");
        }

        if (request.ChannelId.HasValue)
        {
            var channel = await _spaceRepository.GetChannelByIdAsync(
                request.ChannelId.Value,
                cancellationToken);

            if (channel is null)
                throw new KeyNotFoundException("Channel was not found.");

            var member = await _spaceRepository.GetMemberAsync(
                channel.SpaceId,
                currentUserId,
                cancellationToken);

            if (member is null)
            {
                throw new ForbiddenAccessException(
                    "You are not a member of this space.");
            }
        }
        else
        {
            var conversation = await _conversationRepository.GetByIdForMemberAsync(
                request.ConversationId!.Value,
                currentUserId,
                cancellationToken);

            if (conversation is null)
                throw new KeyNotFoundException("Conversation was not found.");
        }

        var now = DateTime.UtcNow;

        foreach (var attachment in message.Attachments)
        {
            _context.AddStorageDeletionOutboxMessage(
                new StorageDeletionOutboxMessage(
                    Guid.NewGuid(),
                    attachment.StorageKey,
                    now));
        }

        _messageRepository.Remove(message);

        await _context.SaveChangesAsync(cancellationToken);
    }
}