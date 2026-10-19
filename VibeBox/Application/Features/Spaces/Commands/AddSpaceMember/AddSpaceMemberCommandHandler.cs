using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Spaces.Commands.AddSpaceMember;

public sealed class AddSpaceMemberCommandHandler
    : IRequestHandler<AddSpaceMemberCommand>
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddSpaceMemberCommandHandler(
        ISpaceRepository spaceRepository,
        IUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _spaceRepository = spaceRepository;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(
        AddSpaceMemberCommand request,
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
            throw new ForbiddenAccessException(
                "Only the space owner or an administrator can add members.");

        var email = request.Email.Trim();

        var user = await _userRepository.GetByEmailAsync(
            email,
            cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("User was not found.");

        var existingMember = await _spaceRepository.GetMemberAsync(
            request.SpaceId,
            user.Id,
            cancellationToken);

        if (existingMember is not null)
            throw new ConflictException(
                "The user is already a member of this space.");

        var member = new SpaceMember(
            space.Id,
            user.Id,
            SpaceRole.Member,
            DateTime.UtcNow);

        await _spaceRepository.AddMemberAsync(
            member,
            cancellationToken);

        await _spaceRepository.SaveChangesAsync(cancellationToken);
    }
}