using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Spaces.Queries.GetSpaceMembers;

public sealed class GetSpaceMembersQueryHandler
    : IRequestHandler<GetSpaceMembersQuery, IReadOnlyList<SpaceMember>>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetSpaceMembersQueryHandler(
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<SpaceMember>> Handle(
        GetSpaceMembersQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var space = await _spaceRepository.GetByIdAsync(
            request.SpaceId,
            cancellationToken);

        if (space is null)
            throw new KeyNotFoundException("Space was not found.");

        var currentMember = await _spaceRepository.GetMemberAsync(
            request.SpaceId,
            currentUserId,
            cancellationToken);

        if (currentMember is null)
            throw new ForbiddenAccessException("You are not a member of this space.");

        return await _spaceRepository.GetMembersAsync(
            request.SpaceId,
            cancellationToken);
    }
}