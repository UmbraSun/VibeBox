using MediatR;

namespace Application.Features.Users.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery
    : IRequest<GetCurrentUserResponse>;