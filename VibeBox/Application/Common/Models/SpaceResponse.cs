namespace Application.Common.Models;

public sealed record SpaceResponse(
    Guid Id,
    string Name,
    Guid OwnerId,
    DateTime CreatedAtUtc);