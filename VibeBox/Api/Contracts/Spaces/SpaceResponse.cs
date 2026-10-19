namespace Api.Contracts.Spaces;

public sealed record SpaceResponse(
    Guid Id,
    string Name,
    Guid OwnerId,
    DateTime CreatedAtUtc);