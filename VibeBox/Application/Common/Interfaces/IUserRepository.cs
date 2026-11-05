using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Represents a repository for managing user entities.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Checks if a user with the specified email exists in the repository.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new user to the repository.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddAsync(
        User user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a user by their email from the repository.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a user by their unique identifier from the repository.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Searches for users by their email, excluding a specific user ID, and limits the number of results returned.
    /// </summary>
    /// <param name="email">The email to search for.</param>
    /// <param name="excludedUserId">The ID of the user to exclude from the search.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task<IReadOnlyList<User>> SearchByEmailAsync(
        string email,
        Guid excludedUserId,
        int limit,
        CancellationToken cancellationToken);
}