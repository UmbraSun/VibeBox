namespace Application.Features.Rooms.Queries.GetMyRooms;

public sealed record MyRoomResponse(
    Guid Id,
    string Name,
    DateTime CreatedAtUtc);