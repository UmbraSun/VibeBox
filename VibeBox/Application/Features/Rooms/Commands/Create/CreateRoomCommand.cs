using MediatR;

namespace Application.Features.Rooms.Commands.Create;

public sealed record CreateRoomCommand(
    string Name) : IRequest<CreateRoomResponse>;