using Domain.Enums;

namespace Application.Common.Models;

public sealed record ChannelResponse(
    Guid Id,
    Guid SpaceId,
    string Name,
    ChannelType Type,
    DateTime CreatedAtUtc);