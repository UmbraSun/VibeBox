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
}