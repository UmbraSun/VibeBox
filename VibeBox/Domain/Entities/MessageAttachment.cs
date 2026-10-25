namespace Domain.Entities;

public sealed class MessageAttachment
{
    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    public string FileName { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    public string StorageKey { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public Message Message { get; private set; } = null!;

    private MessageAttachment()
    {
    }

    public MessageAttachment(
        Guid id,
        Guid messageId,
        string fileName,
        string contentType,
        long sizeBytes,
        string storageKey,
        DateTime createdAtUtc)
    {
        Id = id;
        MessageId = messageId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        CreatedAtUtc = createdAtUtc;
    }
}