namespace Domain.Entities;

public sealed class Message
{
    public Guid Id { get; private set; }

    public Guid? ChannelId { get; private set; }

    public Guid? ConversationId { get; private set; }

    public Guid SenderId { get; private set; }

    public Guid? ReplyToMessageId { get; private set; }

    public string? Content { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Channel? Channel { get; private set; }

    public Conversation? Conversation { get; private set; }

    public User Sender { get; private set; } = null!;
    
    public Message? ReplyToMessage { get; private set; }

    public ICollection<MessageAttachment> Attachments { get; private set; }
        = new List<MessageAttachment>();

    public ICollection<Message> Replies { get; private set; }
        = new List<Message>();

    private Message() { }

    // Creates a message for a text channel.
    public Message(
        Guid id,
        Guid channelId,
        Guid senderId,
        string? content,
        DateTime createdAtUtc,
        Guid? replyToMessageId = null)
    {
        Id = id;
        ChannelId = channelId;
        SenderId = senderId;
        Content = content;
        CreatedAtUtc = createdAtUtc;
        ReplyToMessageId = replyToMessageId;
    }

    // Creates a message for a direct or group conversation.
    public static Message CreateForConversation(
        Guid id,
        Guid conversationId,
        Guid senderId,
        string? content,
        DateTime createdAtUtc,
        Guid? replyToMessageId = null)
    {
        return new Message
        {
            Id = id,
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            CreatedAtUtc = createdAtUtc,
            ReplyToMessageId = replyToMessageId
        };
    }

    public void UpdateContent(string content, DateTime updatedAtUtc)
    {
        Content = content;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void AddAttachment(MessageAttachment attachment)
    {
        if (attachment.MessageId != Id)
            throw new ArgumentException(
                "The attachment belongs to a different message.",
                nameof(attachment));

        Attachments.Add(attachment);
    }
}