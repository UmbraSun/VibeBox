using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Represents a repository for managing Room entities.
/// </summary>
public interface IRoomRepository
{
    /// <summary>
    /// Adds a new Room entity to the repository asynchronously.
    /// </summary>
    /// <param name="room"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddAsync(
        Room room,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves changes made in the repository to the underlying data store asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of Room entities associated with the specified owner ID asynchronously.
    /// </summary>
    /// <param name="ownerId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<Room>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a Room entity by its ID and owner ID asynchronously. Returns null if not found.
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="ownerId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Room?> GetByIdAndOwnerIdAsync(
        Guid roomId,
        Guid ownerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of Room entities associated with the specified participant ID asynchronously.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<Room>> GetByParticipantIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}