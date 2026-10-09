using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Rooms.Queries.GetMyRooms;

public sealed class GetMyRoomsHandler
    : IRequestHandler<GetMyRoomsQuery, IReadOnlyList<MyRoomResponse>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyRoomsHandler(
        IRoomRepository roomRepository,
        ICurrentUserService currentUserService)
    {
        _roomRepository = roomRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<MyRoomResponse>> Handle(
        GetMyRoomsQuery request,
        CancellationToken cancellationToken)
    {
        var rooms = await _roomRepository.GetByParticipantIdAsync(
            _currentUserService.UserId,
            cancellationToken);

        return rooms
            .Select(room => new MyRoomResponse(
                room.Id,
                room.Name,
                room.CreatedAtUtc))
            .ToList();
    }
}