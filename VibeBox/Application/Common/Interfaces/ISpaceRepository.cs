using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Provides asynchronous repository operations for storing, retrieving, and persisting Space entities.
/// </summary>
/// <remarks>Implementations handle persistence concerns and may buffer changes until SaveChangesAsync is invoked.
/// All operations accept a CancellationToken and callers should propagate cancellation. Implementations should document
/// their thread-safety guarantees.
/// </remarks>
public interface ISpaceRepository
{
    /// <summary>
    /// Adds the specified space asynchronously.
    /// </summary>
    /// <param name="space">The space to add.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task AddAsync(
        Space space,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the spaces associated with the specified user.
    /// </summary>
    /// <param name="userId">Identifier of the user whose spaces to retrieve.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of spaces
    /// associated with the user.</returns>
    Task<IReadOnlyList<Space>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously retrieves the space with the specified identifier.
    /// </summary>
    /// <param name="spaceId">Identifier of the space to retrieve.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the Space with the specified
    /// identifier, or null if not found.</returns>
    Task<Space?> GetByIdAsync(
        Guid spaceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the member information for the specified user in the specified space.
    /// </summary>
    /// <param name="spaceId">The identifier of the space.</param>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the SpaceMember if found; otherwise,
    /// null.</returns>
    Task<SpaceMember?> GetMemberAsync(
        Guid spaceId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the members of the specified space.
    /// </summary>
    /// <param name="spaceId">Identifier of the space whose members to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of SpaceMember.</returns>
    Task<IReadOnlyList<SpaceMember>> GetMembersAsync(
        Guid spaceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds the specified member to the space asynchronously.
    /// </summary>
    /// <param name="member">The member to add to the space.</param>
    /// <param name="cancellationToken">A cancellation token to observe while performing the operation.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task AddMemberAsync(
        SpaceMember member,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the specified member from the space.
    /// </summary>
    /// <param name="member">The member to remove from the space.</param>
    void RemoveMember(SpaceMember member);

    /// <summary>
    /// Adds the specified channel to the space asynchronously.
    /// </summary>
    /// <param name="channel">The channel to add to the space.</param>
    /// <param name="cancellationToken">A cancellation token to observe while performing the operation.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task AddChannelAsync(
        Channel channel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the channels of the specified space.
    /// </summary>
    /// <param name="spaceId">Identifier of the space whose channels to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of Channel.</returns>

    Task<IReadOnlyList<Channel>> GetChannelsAsync(
        Guid spaceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously saves all changes made in the current context to the underlying data store.
    /// </summary>
    /// <remarks>May throw update-related exceptions for failure conditions or OperationCanceledException if
    /// cancellation is requested.</remarks>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}