using Application.Common.Interfaces;
using Application.Features.Rooms.Participants;
using MediatR;

namespace Application.Features.Rooms.Queries.GetParticipants;

public sealed class GetRoomParticipantsHandler
    : IRequestHandler<
        GetRoomParticipantsQuery,
        IReadOnlyList<RoomParticipantResponse>>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomParticipantRepository _participantRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRoomParticipantsHandler(
        IRoomRepository roomRepository,
        IRoomParticipantRepository participantRepository,
        ICurrentUserService currentUserService)
    {
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<RoomParticipantResponse>> Handle(
        GetRoomParticipantsQuery request,
        CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAndOwnerIdAsync(
            request.RoomId,
            _currentUserService.UserId,
            cancellationToken);

        if (room is null)
            throw new KeyNotFoundException("Room was not found.");

        var participants = await _participantRepository.GetByRoomIdAsync(
            request.RoomId,
            cancellationToken);

        return participants
            .Select(x => new RoomParticipantResponse(
                x.UserId,
                x.User.Email,
                x.JoinedAtUtc))
            .ToList();
    }
}