using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Represents a repository for managing room participants in the application.
/// </summary>
public interface IRoomParticipantRepository
{
    /// <summary>
    /// Checks if a participant exists in a specific room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> ExistsAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new participant to a room.
    /// </summary>
    /// <param name="participant"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddAsync(
        RoomParticipant participant,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of participants in a specific room.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<RoomParticipant>> GetByRoomIdAsync(
        Guid roomId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves changes made to the repository asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a participant by room ID and user ID.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<RoomParticipant?> GetByRoomIdAndUserIdAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes a participant from the repository.
    /// </summary>
    /// <param name="participant"></param>
    void Remove(RoomParticipant participant);
}