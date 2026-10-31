namespace Domain.Entities;

public sealed class StorageDeletionOutboxMessage
{
    public Guid Id { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime NextAttemptAtUtc { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    private StorageDeletionOutboxMessage()
    {
    }

    public StorageDeletionOutboxMessage(
        Guid id,
        string storageKey,
        DateTime createdAtUtc)
    {
        Id = id;
        StorageKey = storageKey;
        CreatedAtUtc = createdAtUtc;
        NextAttemptAtUtc = createdAtUtc;
    }
}