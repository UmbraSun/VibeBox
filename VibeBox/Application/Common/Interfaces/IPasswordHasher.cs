namespace Application.Common.Interfaces;

/// <summary>
/// Interface for password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the given password and returns the hashed value.
    /// </summary>
    /// <param name="password"></param>
    /// <returns></returns>
    string Hash(string password);

    /// <summary>
    /// Verifies if the given password matches the provided hashed password.
    /// </summary>
    /// <param name="password"></param>
    /// <param name="passwordHash"></param>
    /// <returns></returns>
    bool Verify(string password, string passwordHash);
}