using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.Create;

public sealed class CreateRoomHandler
    : IRequestHandler<CreateRoomCommand, CreateRoomResponse>
{
    private readonly IRoomRepository _roomRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateRoomHandler(
        IRoomRepository roomRepository,
        ICurrentUserService currentUserService)
    {
        _roomRepository = roomRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CreateRoomResponse> Handle(
        CreateRoomCommand request,
        CancellationToken cancellationToken)
    {
        var ownerId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        var room = new Room(
            Guid.NewGuid(),
            ownerId,
            request.Name.Trim(),
            now);

        await _roomRepository.AddAsync(
            room,
            cancellationToken);

        await _roomRepository.SaveChangesAsync(
            cancellationToken);

        return new CreateRoomResponse(
            room.Id,
            room.OwnerId,
            room.Name,
            room.CreatedAtUtc);
    }
}