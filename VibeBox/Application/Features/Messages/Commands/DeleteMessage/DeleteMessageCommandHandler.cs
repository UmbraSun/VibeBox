using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Messages.Commands.DeleteMessage;

public sealed class DeleteMessageCommandHandler
    : IRequestHandler<DeleteMessageCommand>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public DeleteMessageCommandHandler(
        IMessageRepository messageRepository,
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext context)
    {
        _messageRepository = messageRepository;
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task Handle(
        DeleteMessageCommand request,
        CancellationToken cancellationToken)
    {
        var message = await _messageRepository.GetByIdAsync(
            request.MessageId,
            cancellationToken);

        if (message is null || message.ChannelId != request.ChannelId)
            throw new KeyNotFoundException("Message was not found.");

        if (message.SenderId != _currentUserService.UserId)
            throw new ForbiddenAccessException(
                "You can only delete your own messages.");

        var channel = await _spaceRepository.GetChannelByIdAsync(
            request.ChannelId,
            cancellationToken);

        if (channel is null)
            throw new KeyNotFoundException("Channel was not found.");

        var member = await _spaceRepository.GetMemberAsync(
            channel.SpaceId,
            _currentUserService.UserId,
            cancellationToken);

        if (member is null)
            throw new ForbiddenAccessException(
                "You are not a member of this space.");

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