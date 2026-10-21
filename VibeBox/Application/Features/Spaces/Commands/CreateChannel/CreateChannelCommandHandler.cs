using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Spaces.Commands.CreateChannel;

public sealed class CreateChannelCommandHandler
    : IRequestHandler<CreateChannelCommand, Guid>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateChannelCommandHandler(
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateChannelCommand request,
        CancellationToken cancellationToken)
    {
        var space = await _spaceRepository.GetByIdAsync(
            request.SpaceId,
            cancellationToken);

        if (space is null)
        {
            throw new KeyNotFoundException("Space was not found.");
        }

        var currentMember = await _spaceRepository.GetMemberAsync(
            request.SpaceId,
            _currentUserService.UserId,
            cancellationToken);

        if (currentMember is null ||
            currentMember.Role is not (SpaceRole.Owner or SpaceRole.Admin))
        {
            throw new ForbiddenAccessException(
                "Only the space owner or an administrator can create channels.");
        }

        var channel = new Channel(
            Guid.NewGuid(),
            space.Id,
            request.Name.Trim(),
            request.Type,
            DateTime.UtcNow);

        await _spaceRepository.AddChannelAsync(
            channel,
            cancellationToken);

        await _spaceRepository.SaveChangesAsync(cancellationToken);

        return channel.Id;
    }
}