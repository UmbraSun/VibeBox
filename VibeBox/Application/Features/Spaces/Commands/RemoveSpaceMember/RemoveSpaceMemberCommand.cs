using MediatR;

namespace Application.Features.Spaces.Commands.RemoveSpaceMember;

public sealed record RemoveSpaceMemberCommand(
    Guid SpaceId,
    Guid UserId) : IRequest;