using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Represents the contract for the application's database context.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Gets the collection of users in the database.
    /// </summary>
    IQueryable<User> Users { get; }

    /// <summary>
    /// Adds a new storage deletion outbox message to the database context.
    /// </summary>
    /// <param name="message"></param>
    void AddStorageDeletionOutboxMessage(
        StorageDeletionOutboxMessage message);

    /// <summary>
    /// Saves the changes made in the context to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken);
}