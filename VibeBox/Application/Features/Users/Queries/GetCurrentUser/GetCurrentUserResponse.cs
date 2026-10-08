namespace Application.Features.Users.Queries.GetCurrentUser;

public sealed record GetCurrentUserResponse(
    Guid Id,
    string Email,
    DateTime CreatedAtUtc);