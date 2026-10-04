using MediatR;

namespace Application.Features.Users.Commands.Register;

public sealed record RegisterUserCommand(
    string Email,
    string Password) : IRequest<Guid>;