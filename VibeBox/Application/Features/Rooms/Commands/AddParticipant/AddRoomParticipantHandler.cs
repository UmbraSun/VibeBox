using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Features.Rooms.Participants;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.AddParticipant;

public sealed class AddRoomParticipantHandler
    : IRequestHandler<AddRoomParticipantCommand, RoomParticipantResponse>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoomParticipantRepository _participantRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddRoomParticipantHandler(
        IRoomRepository roomRepository,
        IUserRepository userRepository,
        IRoomParticipantRepository participantRepository,
        ICurrentUserService currentUserService)
    {
        _roomRepository = roomRepository;
        _userRepository = userRepository;
        _participantRepository = participantRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RoomParticipantResponse> Handle(
        AddRoomParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAndOwnerIdAsync(
            request.RoomId,
            _currentUserService.UserId,
            cancellationToken);

        if (room is null)
            throw new KeyNotFoundException("Room was not found.");

        var user = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("User was not found.");

        if (await _participantRepository.ExistsAsync(
                request.RoomId,
                request.UserId,
                cancellationToken))
            throw new ConflictException("User is already a participant of this room.");

        var now = DateTime.UtcNow;

        var participant = new RoomParticipant(
            request.RoomId,
            request.UserId,
            now);

        await _participantRepository.AddAsync(
            participant,
            cancellationToken);

        await _participantRepository.SaveChangesAsync(
            cancellationToken);

        return new RoomParticipantResponse(
            user.Id,
            user.Email,
            now);
    }
}