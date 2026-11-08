using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Contains methods for managing conversations, including adding new conversations, retrieving conversations by ID, and finding direct conversations between users.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Adds a new conversation to the repository.
    /// </summary>
    /// <param name="conversation">The conversation to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a conversation by its unique identifier.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Conversation?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a conversation by its unique identifier, including the specified user as a member.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Conversation?> GetByIdForMemberAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all conversations belonging to the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IReadOnlyList<Conversation>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a direct conversation between two users, if it exists.
    /// </summary>
    /// <param name="firstUserId">The unique identifier of the first user.</param>
    /// <param name="secondUserId">The unique identifier of the second user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Conversation?> GetDirectAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets a direct conversation between two users for update, if it exists. This method is intended for scenarios where the conversation may be modified after retrieval.
    /// </summary>
    /// <param name="firstUserId">The unique identifier of the first user.</param>
    /// <param name="secondUserId">The unique identifier of the second user.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Conversation?> GetDirectForUpdateAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a direct conversation between two users if it does not already exist. If the conversation exists, it returns the existing conversation's ID; otherwise, it creates a new conversation and returns its ID.
    /// </summary>
    /// <param name="conversation">The conversation to create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Guid> CreateDirectIfNotExistsAsync(
        Conversation conversation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the IDs of all members in a specific conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IReadOnlyList<Guid>> GetMemberIdsAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new group conversation with the specified details and returns its unique identifier. This method is intended for scenarios where a new group conversation needs to be created, and it ensures that the conversation is properly initialized and stored in the repository.
    /// </summary>
    /// <param name="conversation">The conversation to create.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<Guid> CreateGroupAsync(
        Conversation conversation,
        CancellationToken cancellationToken);
}