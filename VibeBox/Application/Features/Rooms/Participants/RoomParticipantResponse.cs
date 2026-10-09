namespace Application.Features.Rooms.Participants;

public sealed record RoomParticipantResponse(
    Guid UserId,
    string Email,
    DateTime JoinedAtUtc);