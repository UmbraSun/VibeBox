using MediatR;

namespace Application.Features.Spaces.Commands.AddSpaceMember;

public sealed record AddSpaceMemberCommand(
    Guid SpaceId,
    string Email) : IRequest;