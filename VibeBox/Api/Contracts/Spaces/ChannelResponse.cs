using Domain.Enums;

namespace Api.Contracts.Spaces;

public sealed record ChannelResponse(
    Guid Id,
    Guid SpaceId,
    string Name,
    ChannelType Type,
    DateTime CreatedAtUtc);