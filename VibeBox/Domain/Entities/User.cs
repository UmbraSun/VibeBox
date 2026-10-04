namespace Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    private User()
    {
    }

    public User(
        Guid id,
        string email,
        string passwordHash,
        DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }
}