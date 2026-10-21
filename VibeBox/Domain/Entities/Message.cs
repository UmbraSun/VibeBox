namespace Domain.Entities;

public sealed class Message
{
    public Guid Id { get; private set; }
    public Guid ChannelId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public Channel Channel { get; private set; } = null!;
    public User Sender { get; private set; } = null!;

    private Message() { }

    public Message(
        Guid id,
        Guid channelId,
        Guid senderId,
        string content,
        DateTime createdAtUtc)
    {
        Id = id;
        ChannelId = channelId;
        SenderId = senderId;
        Content = content;
        CreatedAtUtc = createdAtUtc;
    }

    public void UpdateContent(string content, DateTime updatedAtUtc)
    {
        Content = content;
        UpdatedAtUtc = updatedAtUtc;
    }
}