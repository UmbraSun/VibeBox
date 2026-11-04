namespace Domain.Entities;

public sealed class ConversationMember
{
    public Guid ConversationId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public DateTime? LastReadAtUtc { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    public User User { get; private set; } = null!;

    private ConversationMember() { }

    public ConversationMember(
        Guid conversationId,
        Guid userId,
        DateTime joinedAtUtc)
    {
        ConversationId = conversationId;
        UserId = userId;
        JoinedAtUtc = joinedAtUtc;
    }

    public void MarkAsRead(DateTime readAtUtc)
    {
        if (LastReadAtUtc is null || readAtUtc > LastReadAtUtc)
            LastReadAtUtc = readAtUtc;
    }
}