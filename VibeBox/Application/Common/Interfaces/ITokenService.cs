using Domain.Entities;

namespace Application.Common.Interfaces;

/// <summary>
/// Represents a service for generating and hashing tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Creates an access token for the specified user.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    string CreateAccessToken(User user);

    /// <summary>
    /// Creates a refresh token.
    /// </summary>
    /// <returns></returns>
    string CreateRefreshToken();

    /// <summary>
    /// Hashes the specified refresh token.
    /// </summary>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    string HashRefreshToken(string refreshToken);
}