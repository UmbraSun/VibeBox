using Domain.Entities;
using MediatR;

namespace Application.Features.Spaces.Queries.GetSpaceChannels;

public sealed record GetSpaceChannelsQuery(Guid SpaceId)
    : IRequest<IReadOnlyList<Channel>>;