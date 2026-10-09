namespace Domain.Entities;

public sealed class Room
{
    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Name { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    public User Owner { get; private set; } = null!;

    public ICollection<RoomParticipant> Participants { get; private set; }
        = new List<RoomParticipant>();

    private Room()
    {
    }

    public Room(
        Guid id,
        Guid ownerId,
        string name,
        DateTime createdAtUtc)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        CreatedAtUtc = createdAtUtc;

        Participants.Add(
            new RoomParticipant(
                id,
                ownerId,
                createdAtUtc));
    }
}