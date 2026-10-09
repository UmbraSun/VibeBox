using Application.Features.Rooms.Participants;
using MediatR;

namespace Application.Features.Rooms.Commands.AddParticipant;

public sealed record AddRoomParticipantCommand(
    Guid RoomId,
    Guid UserId) : IRequest<RoomParticipantResponse>;