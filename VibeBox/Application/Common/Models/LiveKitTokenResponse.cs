namespace Application.Common.Models;

public sealed record LiveKitTokenResponse(
    string ServerUrl,
    string ParticipantToken,
    string RoomName);