using Domain.Entities;
using MediatR;

namespace Application.Features.Spaces.Queries.GetSpaceMembers;

public sealed record GetSpaceMembersQuery(Guid SpaceId)
    : IRequest<IReadOnlyList<SpaceMember>>;