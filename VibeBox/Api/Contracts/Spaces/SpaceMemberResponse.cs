namespace Api.Contracts.Spaces;

public sealed record SpaceMemberResponse(
    Guid UserId,
    string Email,
    string Role,
    DateTime JoinedAtUtc);