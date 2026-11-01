namespace Application.Common.Models;

public sealed record LiveKitTokenResult(
    string ServerUrl,
    string ParticipantToken,
    string RoomName);