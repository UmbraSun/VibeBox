using MediatR;

namespace Application.Features.Users.Queries.SearchUsers;

public sealed record SearchUsersQuery(
    string Email) : IRequest<IReadOnlyList<SearchUsersResponse>>;