using Domain.Enums;

namespace Domain.Entities;

public sealed class SpaceMember
{
    public Guid SpaceId { get; private set; }

    public Guid UserId { get; private set; }

    public SpaceRole Role { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public Space Space { get; private set; } = null!;

    public User User { get; private set; } = null!;

    private SpaceMember()
    {
    }

    public SpaceMember(
        Guid spaceId,
        Guid userId,
        SpaceRole role,
        DateTime joinedAtUtc)
    {
        SpaceId = spaceId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = joinedAtUtc;
    }
}