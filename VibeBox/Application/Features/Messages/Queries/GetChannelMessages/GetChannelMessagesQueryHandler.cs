using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Messages.Queries.GetChannelMessages;

public sealed class GetChannelMessagesQueryHandler
    : IRequestHandler<GetChannelMessagesQuery, IReadOnlyList<Message>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetChannelMessagesQueryHandler(
        IMessageRepository messageRepository,
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _messageRepository = messageRepository;
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<Message>> Handle(
        GetChannelMessagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Limit is < 1 or > 100)
        {
            throw new FluentValidation.ValidationException(
                "Limit must be between 1 and 100.");
        }

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
                "Messages can only be retrieved from text channels.");
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

        return await _messageRepository.GetByChannelAsync(
            channel.Id,
            request.BeforeMessageId,
            request.Limit,
            cancellationToken);
    }
}