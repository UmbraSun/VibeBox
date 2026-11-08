using Domain.Enums;

namespace Domain.Entities;

public sealed class Conversation
{
    public Guid Id { get; private set; }

    public ConversationType Type { get; private set; }

    public string? Name { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastMessageAtUtc { get; private set; }

    public Guid? DirectUserLowId { get; private set; }
    public Guid? DirectUserHighId { get; private set; }

    public ICollection<ConversationMember> Members { get; private set; }
        = new List<ConversationMember>();

    public ICollection<Message> Messages { get; private set; }
        = new List<Message>();

    private Conversation() { }

    public Conversation(
        Guid id,
        ConversationType type,
        string? name,
        DateTime createdAtUtc)
    {
        if (type == ConversationType.Direct && !string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Direct conversations cannot have a name.",
                nameof(name));

        if (type == ConversationType.Group && string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Group conversations must have a name.",
                nameof(name));

        Id = id;
        Type = type;
        Name = name?.Trim();
        CreatedAtUtc = createdAtUtc;
    }

    public static Conversation CreateDirect(
        Guid id,
        Guid firstUserId,
        Guid secondUserId,
        DateTime createdAtUtc)
    {
        if (firstUserId == secondUserId)
        {
            throw new ArgumentException(
                "A direct conversation requires two different users.");
        }

        var conversation = new Conversation(
            id,
            ConversationType.Direct,
            null,
            createdAtUtc);

        conversation.DirectUserLowId =
            firstUserId.CompareTo(secondUserId) < 0
                ? firstUserId
                : secondUserId;

        conversation.DirectUserHighId =
            firstUserId.CompareTo(secondUserId) < 0
                ? secondUserId
                : firstUserId;

        return conversation;
    }

    public void AddMember(ConversationMember member)
    {
        if (member.ConversationId != Id)
            throw new ArgumentException(
                "The member belongs to a different conversation.",
                nameof(member));

        if (Members.Any(x => x.UserId == member.UserId))
            throw new InvalidOperationException(
                "The user is already a member of this conversation.");

        Members.Add(member);
    }

    public void UpdateLastMessageAt(DateTime createdAtUtc)
    {
        if (LastMessageAtUtc is null || createdAtUtc > LastMessageAtUtc)
            LastMessageAtUtc = createdAtUtc;
    }

    public static Conversation CreateGroup(
        Guid id,
        string name,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A group conversation must have a name.",
                nameof(name));
        }

        return new Conversation(
            id,
            ConversationType.Group,
            name,
            createdAtUtc);
    }

    public void RemoveMember(Guid userId)
    {
        var member = Members.FirstOrDefault(x => x.UserId == userId);

        if (member is null)
            throw new InvalidOperationException("The user is not a member of this conversation.");

        Members.Remove(member);
    }

    public void Leave(Guid userId)
    {
        RemoveMember(userId);
    }

    public void Rename(string name)
    {
        if (Type != ConversationType.Group)
            throw new InvalidOperationException("Only group conversations can be renamed.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A group name is required.", nameof(name));

        var normalizedName = name.Trim();

        if (normalizedName.Length > 200)
            throw new ArgumentException("The group name cannot exceed 200 characters.", nameof(name));

        Name = normalizedName;
    }
}