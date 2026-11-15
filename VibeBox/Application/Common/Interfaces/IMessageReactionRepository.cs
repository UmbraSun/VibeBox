
using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Provides persistence operations for message reactions.
/// </summary>
public interface IMessageReactionRepository
{
    /// <summary>
    /// Retrieves a message reaction by message, user, and emoji.
    /// </summary>
    /// <param name="messageId">The identifier of the message.</param>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="emoji">The emoji associated with the reaction.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching message reaction, or <c>null</c> if none is found.</returns>
    Task<MessageReaction?> GetByIdAsync(
        Guid messageId,
        Guid userId,
        string emoji,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a message reaction.
    /// </summary>
    /// <param name="reaction">The message reaction to add.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(
        MessageReaction reaction,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the specified message reaction.
    /// </summary>
    /// <param name="reaction">The message reaction to remove.</param>
    void Remove(MessageReaction reaction);

    /// <summary>
    /// Retrieves reactions for the specified message IDs.
    /// </summary>
    /// <param name="messageIds">The IDs of the messages whose reactions are retrieved.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of reactions associated with the specified messages.</returns>
    Task<IReadOnlyList<MessageReaction>> GetByMessageIdsAsync(
        IReadOnlyCollection<Guid> messageIds,
        CancellationToken cancellationToken);
}
