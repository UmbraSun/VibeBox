using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomById;

public sealed record GetRoomByIdQuery(
    Guid RoomId) : IRequest<RoomDetailsResponse>;