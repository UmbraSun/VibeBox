using MediatR;

namespace Application.Features.Users.Commands.Login;

public sealed record LoginUserCommand(
    string Email,
    string Password) : IRequest<LoginResponse>;