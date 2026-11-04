namespace Application.Common.Models;

public sealed record SpaceMemberResponse(
    Guid UserId,
    string Email,
    string Role,
    DateTime JoinedAtUtc);