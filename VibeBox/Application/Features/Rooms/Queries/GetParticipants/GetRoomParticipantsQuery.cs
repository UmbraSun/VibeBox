using Application.Features.Rooms.Participants;
using MediatR;

namespace Application.Features.Rooms.Queries.GetParticipants;

public sealed record GetRoomParticipantsQuery(
    Guid RoomId) : IRequest<IReadOnlyList<RoomParticipantResponse>>;