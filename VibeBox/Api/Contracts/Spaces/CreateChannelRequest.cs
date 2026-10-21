using Domain.Entities;
using Domain.Enums;

namespace Api.Contracts.Spaces;

public sealed record CreateChannelRequest(
    string Name,
    ChannelType Type);