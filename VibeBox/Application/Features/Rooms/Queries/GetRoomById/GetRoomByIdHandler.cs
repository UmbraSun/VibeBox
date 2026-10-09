using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomById;

public sealed class GetRoomByIdHandler
    : IRequestHandler<GetRoomByIdQuery, RoomDetailsResponse>
{
    private readonly IRoomRepository _roomRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRoomByIdHandler(
        IRoomRepository roomRepository,
        ICurrentUserService currentUserService)
    {
        _roomRepository = roomRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RoomDetailsResponse> Handle(
        GetRoomByIdQuery request,
        CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAndOwnerIdAsync(
            request.RoomId,
            _currentUserService.UserId,
            cancellationToken);

        if (room is null)
            throw new KeyNotFoundException("Room was not found.");

        return new RoomDetailsResponse(
            room.Id,
            room.Name,
            room.CreatedAtUtc);
    }
}