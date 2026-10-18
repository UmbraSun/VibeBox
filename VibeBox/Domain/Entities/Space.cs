using Domain.Enums;
using System.Threading.Channels;

namespace Domain.Entities;

public sealed class Space
{
    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public User Owner { get; private set; } = null!;

    public ICollection<SpaceMember> Members { get; private set; }
        = new List<SpaceMember>();

    public ICollection<Channel> Channels { get; private set; }
        = new List<Channel>();

    private Space()
    {
    }

    public Space(
        Guid id,
        Guid ownerId,
        string name,
        DateTime createdAtUtc)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        CreatedAtUtc = createdAtUtc;

        Members.Add(new SpaceMember(
            id,
            ownerId,
            SpaceRole.Owner,
            createdAtUtc));
    }
}