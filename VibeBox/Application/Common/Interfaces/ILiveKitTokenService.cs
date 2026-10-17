namespace Application.Common.Interfaces;

/// <summary>
/// Defines a contract for creating access tokens that grant users access to LiveKit rooms.
/// </summary>
/// <remarks>Implementations should produce short-lived, cryptographically signed tokens that encode the room and
/// user identifiers. Responsibility for token lifetime, signing keys, and claim composition rests with the
/// implementation.</remarks>
public interface ILiveKitTokenService
{
    /// <summary>
    /// Creates an access token for a user to join a specific LiveKit room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    string CreateToken(Guid roomId, Guid userId);
}