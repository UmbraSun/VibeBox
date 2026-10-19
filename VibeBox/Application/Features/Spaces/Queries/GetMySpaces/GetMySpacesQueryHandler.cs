using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Spaces.Queries.GetMySpaces;

public sealed class GetMySpacesQueryHandler
    : IRequestHandler<GetMySpacesQuery, IReadOnlyList<Space>>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMySpacesQueryHandler(
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public Task<IReadOnlyList<Space>> Handle(
        GetMySpacesQuery request,
        CancellationToken cancellationToken)
    {
        return _spaceRepository.GetByUserIdAsync(
            _currentUserService.UserId,
            cancellationToken);
    }
}