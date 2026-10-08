namespace Application.Common.Interfaces;

/// <summary>
/// Represents a service that provides information about the current user.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Current user's unique identifier.
    /// </summary>
    Guid UserId { get; }
}