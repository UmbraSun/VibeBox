using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Messages.Commands.SendMessage;

public sealed class SendMessageCommandHandler
    : IRequestHandler<SendMessageCommand, Guid>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public SendMessageCommandHandler(
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

    public async Task<Guid> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        var channel = await _spaceRepository.GetChannelByIdAsync(
            request.ChannelId,
            cancellationToken);

        if (channel is null)
        {
            throw new KeyNotFoundException("Channel was not found.");
        }

        if (channel.Type != ChannelType.Text)
        {
            throw new ConflictException(
                "Messages can only be sent to text channels.");
        }

        var member = await _spaceRepository.GetMemberAsync(
            channel.SpaceId,
            _currentUserService.UserId,
            cancellationToken);

        if (member is null)
        {
            throw new ForbiddenAccessException(
                "You are not a member of this space.");
        }

        var message = new Message(
            Guid.NewGuid(),
            channel.Id,
            _currentUserService.UserId,
            request.Content.Trim(),
            DateTime.UtcNow);

        await _messageRepository.AddAsync(message, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return message.Id;
    }
}