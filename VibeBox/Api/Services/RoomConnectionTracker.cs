namespace Api.Services;

public sealed record RoomConnectionMembership(Guid RoomId, Guid UserId);

public sealed class RoomConnectionTracker
{
    private readonly object _sync = new();

    private readonly Dictionary<Guid, Dictionary<Guid, HashSet<string>>> _rooms = [];
    private readonly Dictionary<string, HashSet<RoomConnectionMembership>> _connections = new(StringComparer.Ordinal);

    public bool AddConnection(
        Guid roomId,
        Guid userId,
        string connectionId)
    {
        lock (_sync)
        {
            if (!_rooms.TryGetValue(roomId, out var users))
            {
                users = [];
                _rooms.Add(roomId, users);
            }

            if (!users.TryGetValue(userId, out var connections))
            {
                connections = new HashSet<string>(StringComparer.Ordinal);
                users.Add(userId, connections);
            }

            if (!connections.Add(connectionId))
                return false;

            if (!_connections.TryGetValue(connectionId, out var memberships))
            {
                memberships = [];
                _connections.Add(connectionId, memberships);
            }

            memberships.Add(new RoomConnectionMembership(roomId, userId));

            return connections.Count == 1;
        }
    }

    public bool RemoveConnection(
        Guid roomId,
        Guid userId,
        string connectionId)
    {
        lock (_sync)
        {
            if (!_rooms.TryGetValue(roomId, out var users) ||
                !users.TryGetValue(userId, out var connections) ||
                !connections.Remove(connectionId))
                return false;

            RemoveConnectionMembership(
                connectionId,
                new RoomConnectionMembership(roomId, userId));

            if (connections.Count > 0)
                return false;

            users.Remove(userId);

            if (users.Count == 0)
                _rooms.Remove(roomId);

            return true;
        }
    }

    public IReadOnlyList<RoomConnectionMembership> RemoveAllForConnection(
        string connectionId)
    {
        lock (_sync)
        {
            if (!_connections.Remove(connectionId, out var memberships))
            {
                return [];
            }

            var lastConnections = new List<RoomConnectionMembership>();

            foreach (var membership in memberships)
            {
                if (!_rooms.TryGetValue(membership.RoomId, out var users) ||
                    !users.TryGetValue(membership.UserId, out var connections))
                    continue;

                connections.Remove(connectionId);

                if (connections.Count > 0)
                    continue;

                users.Remove(membership.UserId);
                lastConnections.Add(membership);

                if (users.Count == 0)
                    _rooms.Remove(membership.RoomId);
            }

            return lastConnections;
        }
    }

    public IReadOnlyList<string> RemoveUserFromRoom(
        Guid roomId,
        Guid userId)
    {
        lock (_sync)
        {
            if (!_rooms.TryGetValue(roomId, out var users) || users.Remove(userId, out var connections))
                return [];

            var connectionIds = connections.ToArray();

            foreach (var connectionId in connectionIds)
                RemoveConnectionMembership(connectionId, new RoomConnectionMembership(roomId, userId));

            if (users.Count == 0)
                _rooms.Remove(roomId);

            return connectionIds;
        }
    }

    private void RemoveConnectionMembership(
        string connectionId,
        RoomConnectionMembership membership)
    {
        if (!_connections.TryGetValue(connectionId, out var memberships))
            return;

        memberships.Remove(membership);

        if (memberships.Count == 0)
            _connections.Remove(connectionId);
    }
}