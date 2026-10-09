using MediatR;

namespace Application.Features.Rooms.Queries.GetMyRooms;

public sealed record GetMyRoomsQuery
    : IRequest<IReadOnlyList<MyRoomResponse>>;