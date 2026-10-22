using Application.Common.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

[Authorize]
public sealed class MessageHub : Hub
{
    private readonly ISpaceRepository _spaceRepository;

    public MessageHub(ISpaceRepository spaceRepository)
    {
        _spaceRepository = spaceRepository;
    }

    public async Task JoinChannel(Guid channelId)
    {
        var userId = GetUserId();

        var channel = await _spaceRepository.GetChannelByIdAsync(
            channelId,
            Context.ConnectionAborted);

        if (channel is null)
            throw new HubException("Channel was not found.");

        if (channel.Type != ChannelType.Text)
            throw new HubException("Only text channels support messaging.");

        var member = await _spaceRepository.GetMemberAsync(
            channel.SpaceId,
            userId,
            Context.ConnectionAborted);

        if (member is null)
            throw new HubException("You are not a member of this space.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetChannelGroupName(channelId),
            Context.ConnectionAborted);
    }

    public async Task LeaveChannel(Guid channelId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetChannelGroupName(channelId),
            Context.ConnectionAborted);
    }

    public static string GetChannelGroupName(Guid channelId)
        => $"channel:{channelId}";

    private Guid GetUserId()
    {
        var userId = Context.UserIdentifier;

        if (Guid.TryParse(userId, out var parsedUserId))
            return parsedUserId;

        throw new HubException("Authenticated user ID is unavailable.");
    }
}
