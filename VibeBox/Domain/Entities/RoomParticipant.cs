namespace Domain.Entities;

public sealed class RoomParticipant
{
    public Guid RoomId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public Room Room { get; private set; } = null!;

    public User User { get; private set; } = null!;

    private RoomParticipant()
    {
    }

    public RoomParticipant(
        Guid roomId,
        Guid userId,
        DateTime joinedAtUtc)
    {
        RoomId = roomId;
        UserId = userId;
        JoinedAtUtc = joinedAtUtc;
    }
}