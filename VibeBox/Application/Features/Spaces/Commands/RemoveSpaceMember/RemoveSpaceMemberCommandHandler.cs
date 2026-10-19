using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Spaces.Commands.RemoveSpaceMember;

public sealed class RemoveSpaceMemberCommandHandler
    : IRequestHandler<RemoveSpaceMemberCommand>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public RemoveSpaceMemberCommandHandler(
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        RemoveSpaceMemberCommand request,
        CancellationToken cancellationToken)
    {
        var space = await _spaceRepository.GetByIdAsync(
            request.SpaceId,
            cancellationToken);

        if (space is null)
            throw new KeyNotFoundException("Space was not found.");

        var currentUserId = _currentUserService.UserId;

        var currentMember = await _spaceRepository.GetMemberAsync(
            request.SpaceId,
            currentUserId,
            cancellationToken);

        if (currentMember is null ||
            currentMember.Role is not (SpaceRole.Owner or SpaceRole.Admin))
            throw new ForbiddenAccessException("Only the space owner or an administrator can remove members.");

        if (space.OwnerId == request.UserId)
            throw new ConflictException("The owner of a space cannot be removed.");

        var targetMember = await _spaceRepository.GetMemberAsync(
            request.SpaceId,
            request.UserId,
            cancellationToken);

        if (targetMember is null)
            throw new KeyNotFoundException("The user is not a member of this space.");

        _spaceRepository.RemoveMember(targetMember);

        await _spaceRepository.SaveChangesAsync(cancellationToken);
    }
}