using Application.Common.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

[Authorize]
public sealed class MessageHub : Hub
{
    private readonly ISpaceRepository _spaceRepository;
    private readonly IConversationRepository _conversationRepository;

    public MessageHub(
        ISpaceRepository spaceRepository,
        IConversationRepository conversationRepository)
    {
        _spaceRepository = spaceRepository;
        _conversationRepository = conversationRepository;
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

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();

        var conversation = await _conversationRepository.GetByIdForMemberAsync(
            conversationId,
            userId,
            Context.ConnectionAborted);

        if (conversation is null)
            throw new HubException("Conversation was not found.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetConversationGroupName(conversationId),
            Context.ConnectionAborted);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetConversationGroupName(conversationId),
            Context.ConnectionAborted);
    }

    public static string GetChannelGroupName(Guid channelId)
        => $"channel:{channelId}";

    public static string GetConversationGroupName(Guid conversationId)
        => $"conversation:{conversationId}";

    private Guid GetUserId()
    {
        var userId = Context.UserIdentifier;

        if (Guid.TryParse(userId, out var parsedUserId))
            return parsedUserId;

        throw new HubException("Authenticated user ID is unavailable.");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetUserGroupName(userId),
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public static string GetUserGroupName(Guid userId)
        => $"user:{userId}";
}