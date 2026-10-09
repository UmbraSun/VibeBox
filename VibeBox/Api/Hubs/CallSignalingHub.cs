using Api.Services;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Api.Hubs;

[Authorize]
public sealed class CallSignalingHub : Hub<ICallSignalingClient>
{
    private const int MaxSdpLength = 100_000;
    private const int MaxIceCandidateLength = 4_096;

    private readonly RoomConnectionTracker _connectionTracker;

    private readonly IRoomParticipantRepository _participantRepository;

    public CallSignalingHub(
        IRoomParticipantRepository participantRepository,
        RoomConnectionTracker connectionTracker)
    {
        _participantRepository = participantRepository;
        _connectionTracker = connectionTracker;
    }

    public Task SendOffer(
        Guid roomId,
        Guid targetUserId,
        string sdp,
        CancellationToken cancellationToken)
    {
        return SendDescriptionAsync(
            roomId,
            targetUserId,
            sdp,
            isOffer: true,
            cancellationToken);
    }

    public Task SendAnswer(
        Guid roomId,
        Guid targetUserId,
        string sdp,
        CancellationToken cancellationToken)
    {
        return SendDescriptionAsync(
            roomId,
            targetUserId,
            sdp,
            isOffer: false,
            cancellationToken);
    }

    public async Task SendIceCandidate(
        Guid roomId,
        Guid targetUserId,
        string candidate,
        string? sdpMid,
        int? sdpMLineIndex,
        CancellationToken cancellationToken)
    {
        var senderUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > MaxIceCandidateLength)
            throw new HubException("Invalid ICE candidate.");

        await EnsureBothParticipantsAsync(
            roomId,
            senderUserId,
            targetUserId,
            cancellationToken);

        await Clients.User(targetUserId.ToString()).ReceiveIceCandidate(
            roomId,
            senderUserId,
            candidate,
            sdpMid,
            sdpMLineIndex);
    }

    private async Task SendDescriptionAsync(
        Guid roomId,
        Guid targetUserId,
        string sdp,
        bool isOffer,
        CancellationToken cancellationToken)
    {
        var senderUserId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(sdp) || sdp.Length > MaxSdpLength)
            throw new HubException("Invalid session description.");

        await EnsureBothParticipantsAsync(
            roomId,
            senderUserId,
            targetUserId,
            cancellationToken);

        var target = Clients.User(targetUserId.ToString());

        if (isOffer)
            await target.ReceiveOffer(roomId, senderUserId, sdp);
        else
            await target.ReceiveAnswer(roomId, senderUserId, sdp);
    }

    private async Task EnsureBothParticipantsAsync(
        Guid roomId,
        Guid senderUserId,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        if (senderUserId == targetUserId)
            throw new HubException("You cannot send signaling messages to yourself.");

        var senderIsParticipant =
            await _participantRepository.ExistsAsync(
                roomId,
                senderUserId,
                cancellationToken);

        var targetIsParticipant =
            await _participantRepository.ExistsAsync(
                roomId,
                targetUserId,
                cancellationToken);

        if (!senderIsParticipant || !targetIsParticipant)
            throw new HubException("Both users must be participants of the room.");
    }

    private Guid GetCurrentUserId()
    {
        var userId =
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userId, out var parsedUserId))
            throw new HubException("User identity is missing.");

        return parsedUserId;
    }

    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetCurrentUserId();

        var isParticipant = await _participantRepository.ExistsAsync(
            roomId,
            userId,
            Context.ConnectionAborted);

        if (!isParticipant)
            throw new HubException("You are not a participant of this room.");

        var groupName = GetRoomGroupName(roomId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName,
            Context.ConnectionAborted);

        var isFirstConnection = _connectionTracker.AddConnection(
            roomId,
            userId,
            Context.ConnectionId);

        if (isFirstConnection)
            await Clients.OthersInGroup(groupName)
                .ParticipantJoined(roomId, userId);
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var userId = GetCurrentUserId();
        var groupName = GetRoomGroupName(roomId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            groupName,
            Context.ConnectionAborted);

        var isLastConnection = _connectionTracker.RemoveConnection(
            roomId,
            userId,
            Context.ConnectionId);

        if (isLastConnection)
            await Clients.Group(groupName)
                .ParticipantLeft(roomId, userId);
    }

    private static string GetRoomGroupName(Guid roomId)
    {
        return $"room:{roomId}";
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var memberships = _connectionTracker.RemoveAllForConnection(
            Context.ConnectionId);

        foreach (var membership in memberships)
            await Clients.Group(GetRoomGroupName(membership.RoomId))
                .ParticipantLeft(membership.RoomId, membership.UserId);

        await base.OnDisconnectedAsync(exception);
    }
}