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
}