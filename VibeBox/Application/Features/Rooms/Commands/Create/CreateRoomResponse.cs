namespace Application.Features.Rooms.Commands.Create;

public sealed record CreateRoomResponse(
    Guid Id,
    Guid OwnerId,
    string Name,
    DateTime CreatedAtUtc);