using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Spaces.Commands.CreateSpace;

public sealed class CreateSpaceCommandHandler
    : IRequestHandler<CreateSpaceCommand, Guid>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateSpaceCommandHandler(
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService)
    {
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(
        CreateSpaceCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var space = new Space(
            Guid.NewGuid(),
            userId,
            request.Name.Trim(),
            DateTime.UtcNow);

        await _spaceRepository.AddAsync(space, cancellationToken);
        await _spaceRepository.SaveChangesAsync(cancellationToken);

        return space.Id;
    }
}