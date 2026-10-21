using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Spaces.Commands.CreateChannel;

public sealed record CreateChannelCommand(
    Guid SpaceId,
    string Name,
    ChannelType Type) : IRequest<Guid>;