using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Spaces.Queries.GetSpaceChannels;

public sealed class GetSpaceChannelsQueryHandler
    : IRequestHandler<GetSpaceChannelsQuery, IReadOnlyList<Channel>>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetSpaceChannelsQueryHandler(
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<Channel>> Handle(
        GetSpaceChannelsQuery request,
        CancellationToken cancellationToken)
    {
        var space = await _spaceRepository.GetByIdAsync(
            request.SpaceId,
            cancellationToken);

        if (space is null)
            throw new KeyNotFoundException("Space was not found.");

        var member = await _spaceRepository.GetMemberAsync(
            request.SpaceId,
            _currentUserService.UserId,
            cancellationToken);

        if (member is null)
            throw new ForbiddenAccessException("You are not a member of this space.");

        return await _spaceRepository.GetChannelsAsync(
            request.SpaceId,
            cancellationToken);
    }
}