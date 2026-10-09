using MediatR;

namespace Application.Features.Rooms.Commands.RemoveParticipant;

public sealed record RemoveRoomParticipantCommand(
    Guid RoomId,
    Guid UserId) : IRequest<Unit>;