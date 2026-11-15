using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Messages.Commands.ToggleMessageReaction;

public sealed class ToggleMessageReactionCommandHandler
    : IRequestHandler<ToggleMessageReactionCommand, bool>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageReactionRepository _reactionRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public ToggleMessageReactionCommandHandler(
        IMessageRepository messageRepository,
        IMessageReactionRepository reactionRepository,
        ISpaceRepository spaceRepository,
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext context)
    {
        _messageRepository = messageRepository;
        _reactionRepository = reactionRepository;
        _spaceRepository = spaceRepository;
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<bool> Handle(
        ToggleMessageReactionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ChannelId.HasValue == request.ConversationId.HasValue)
            throw new ArgumentException(
                "Exactly one parent identifier must be specified.");

        if (string.IsNullOrWhiteSpace(request.Emoji) ||
            request.Emoji.Length > 32)
            throw new ArgumentException("Invalid emoji.");

        var userId = _currentUserService.UserId;

        var message = await _messageRepository.GetByIdAsync(
            request.MessageId,
            cancellationToken);

        if (message is null)
            throw new KeyNotFoundException("Message not found.");

        if (request.ChannelId.HasValue)
        {
            var channel = await _spaceRepository.GetChannelByIdAsync(
                request.ChannelId.Value,
                cancellationToken);

            if (channel is null || message.ChannelId != channel.Id)
                throw new KeyNotFoundException("Message not found.");

            var member = await _spaceRepository.GetMemberAsync(
                channel.SpaceId,
                userId,
                cancellationToken);

            if (member is null)
                throw new ForbiddenAccessException();
        }
        else
        {
            var conversation = await _conversationRepository.GetByIdForMemberAsync(
                request.ConversationId!.Value,
                userId,
                cancellationToken);

            if (conversation is null ||
                message.ConversationId != request.ConversationId)
                throw new KeyNotFoundException("Message not found.");
        }

        var existingReaction = await _reactionRepository.GetByIdAsync(
            message.Id,
            userId,
            request.Emoji,
            cancellationToken);

        if (existingReaction is not null)
        {
            _reactionRepository.Remove(existingReaction);

            await _context.SaveChangesAsync(cancellationToken);

            return false;
        }

        var reaction = new MessageReaction(
            Guid.NewGuid(),
            message.Id,
            userId,
            request.Emoji,
            DateTime.UtcNow);

        await _reactionRepository.AddAsync(
            reaction,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}