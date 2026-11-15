
namespace Domain.Entities;

public sealed class MessageReaction
{
    public Guid Id { get; private set; }

    public Guid MessageId { get; private set; }

    public Guid UserId { get; private set; }

    public string Emoji { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public Message Message { get; private set; } = null!;

    public User User { get; private set; } = null!;

    private MessageReaction() { }

    public MessageReaction(
        Guid id,
        Guid messageId,
        Guid userId,
        string emoji,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            throw new ArgumentException(
                "Emoji cannot be empty.",
                nameof(emoji));

        if (emoji.Length > 32)
            throw new ArgumentException(
                "Emoji cannot exceed 32 characters.",
                nameof(emoji));

        Id = id;
        MessageId = messageId;
        UserId = userId;
        Emoji = emoji;
        CreatedAtUtc = createdAtUtc;
    }
}
