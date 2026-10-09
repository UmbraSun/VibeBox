namespace Api.Hubs;

/// <summary>
/// Defines the client-side methods for handling call signaling events in a real-time communication scenario.
/// </summary>
public interface ICallSignalingClient
{
    /// <summary>
    /// Handles the reception of an offer from a sender in a specific room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="senderUserId"></param>
    /// <param name="sdp"></param>
    /// <returns></returns>
    Task ReceiveOffer(Guid roomId, Guid senderUserId, string sdp);

    /// <summary>
    /// Handles the reception of an answer from a sender in a specific room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="senderUserId"></param>
    /// <param name="sdp"></param>
    /// <returns></returns>
    Task ReceiveAnswer(Guid roomId, Guid senderUserId, string sdp);

    /// <summary>
    /// Handles the reception of an ICE candidate from a sender in a specific room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="senderUserId"></param>
    /// <param name="candidate"></param>
    /// <param name="sdpMid"></param>
    /// <param name="sdpMLineIndex"></param>
    /// <returns></returns>
    Task ReceiveIceCandidate(
        Guid roomId,
        Guid senderUserId,
        string candidate,
        string? sdpMid,
        int? sdpMLineIndex);

    /// <summary>
    /// Notifies the client that a participant has joined a specific room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task ParticipantJoined(Guid roomId, Guid userId);

    /// <summary>
    /// Notifies the client that a participant has left a specific room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task ParticipantLeft(Guid roomId, Guid userId);
}