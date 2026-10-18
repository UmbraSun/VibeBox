using Domain.Enums;

namespace Domain.Entities;

public sealed class Channel
{
    public Guid Id { get; private set; }

    public Guid SpaceId { get; private set; }

    public string Name { get; private set; } = null!;

    public ChannelType Type { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Space Space { get; private set; } = null!;

    private Channel()
    {
    }

    public Channel(
        Guid id,
        Guid spaceId,
        string name,
        ChannelType type,
        DateTime createdAtUtc)
    {
        Id = id;
        SpaceId = spaceId;
        Name = name;
        Type = type;
        CreatedAtUtc = createdAtUtc;
    }
}