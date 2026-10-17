namespace Api.Contracts.Rooms;

public sealed record LiveKitTokenResponse(
    string ServerUrl,
    string ParticipantToken,
    string RoomName);