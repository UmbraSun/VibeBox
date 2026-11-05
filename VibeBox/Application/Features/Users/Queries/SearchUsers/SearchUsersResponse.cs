namespace Application.Features.Users.Queries.SearchUsers;

public sealed record SearchUsersResponse(
    Guid Id,
    string Email);