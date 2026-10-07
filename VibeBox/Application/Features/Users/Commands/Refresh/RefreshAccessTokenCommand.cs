using Application.Features.Users.Commands.Login;
using MediatR;

namespace Application.Features.Users.Commands.Refresh;

public sealed record RefreshAccessTokenCommand(
    string RefreshToken) : IRequest<LoginResponse>;