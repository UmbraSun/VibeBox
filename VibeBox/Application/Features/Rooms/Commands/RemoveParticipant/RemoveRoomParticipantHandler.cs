using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Rooms.Commands.RemoveParticipant;

public sealed class RemoveRoomParticipantHandler
    : IRequestHandler<RemoveRoomParticipantCommand, Unit>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomParticipantRepository _participantRepository;

    public RemoveRoomParticipantHandler(
        ICurrentUserService currentUserService,
        IRoomRepository roomRepository,
        IRoomParticipantRepository participantRepository)
    {
        _currentUserService = currentUserService;
        _roomRepository = roomRepository;
        _participantRepository = participantRepository;
    }

    public async Task<Unit> Handle(
        RemoveRoomParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAndOwnerIdAsync(
            request.RoomId,
            _currentUserService.UserId,
            cancellationToken);

        if (room is null)
            throw new KeyNotFoundException("Room not found.");

        if (room.OwnerId == request.UserId)
            throw new ConflictException("The room owner cannot be removed as a participant.");

        var participant =
            await _participantRepository.GetByRoomIdAndUserIdAsync(
                request.RoomId,
                request.UserId,
                cancellationToken);

        if (participant is null)
            throw new KeyNotFoundException("Room participant not found.");

        _participantRepository.Remove(participant);

        await _participantRepository.SaveChangesAsync(
            cancellationToken);

        return Unit.Value;
    }
}