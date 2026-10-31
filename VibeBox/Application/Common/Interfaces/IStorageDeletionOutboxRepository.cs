using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Specifies the contract for a repository that manages storage deletion outbox messages, allowing for claiming, marking as processed, and marking as failed.
/// </summary>
public interface IStorageDeletionOutboxRepository
{
    /// <summary>
    /// Tries to claim the next storage deletion outbox message for processing, based on the current time and a specified lock duration. Returns the claimed message if successful, or null if no message is available for claiming.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="lockUntil">The time until which the message should be locked.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The claimed message if successful, or null if no message is available for claiming.</returns>
    Task<StorageDeletionOutboxMessage?> TryClaimNextAsync(
        DateTime now,
        DateTime lockUntil,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks a storage deletion outbox message as processed, updating its processed timestamp. This indicates that the message has been successfully handled and should not be retried.
    /// </summary>
    /// <param name="id">The ID of the message to mark as processed.</param>
    /// <param name="processedAtUtc">The UTC timestamp when the message was processed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task MarkAsProcessedAsync(
        Guid id,
        DateTime processedAtUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks a storage deletion outbox message as failed, updating its next attempt timestamp and recording the error that occurred. This allows for retrying the message processing at a later time.
    /// </summary>
    /// <param name="id">The ID of the message to mark as failed.</param>
    /// <param name="nextAttemptAtUtc">The UTC timestamp when the message should be attempted again.</param>
    /// <param name="error">The error message that occurred.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task MarkAsFailedAsync(
        Guid id,
        DateTime nextAttemptAtUtc,
        string error,
        CancellationToken cancellationToken);
}