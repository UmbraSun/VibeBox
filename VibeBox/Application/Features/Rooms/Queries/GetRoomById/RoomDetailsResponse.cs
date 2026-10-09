namespace Application.Features.Rooms.Queries.GetRoomById;

public sealed record RoomDetailsResponse(
    Guid Id,
    string Name,
    DateTime CreatedAtUtc);