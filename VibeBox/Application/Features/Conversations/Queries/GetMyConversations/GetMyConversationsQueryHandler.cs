using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.Conversations.Queries.GetMyConversations;

public sealed class GetMyConversationsQueryHandler
    : IRequestHandler<GetMyConversationsQuery, IReadOnlyList<ConversationSummaryResponse>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetMyConversationsQueryHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ConversationSummaryResponse>> Handle(
        GetMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var conversations = await _conversationRepository.GetByUserIdAsync(
            currentUserId,
            cancellationToken);

        var unreadCounts = await _conversationRepository.GetUnreadCountsByUserIdAsync(
            currentUserId,
            cancellationToken);

        return conversations.Select(conversation =>
        {
            var currentMember = conversation.Members
                .First(member => member.UserId == currentUserId);

            var otherMember = conversation.Type == ConversationType.Direct
                ? conversation.Members.FirstOrDefault(
                    member => member.UserId != currentUserId)
                : null;

            var lastMessage = conversation.Messages
                .OrderByDescending(message => message.CreatedAtUtc)
                .ThenByDescending(message => message.Id)
                .FirstOrDefault();

            var unreadCount = unreadCounts.TryGetValue(
                conversation.Id,
                out var count)
                    ? count
                    : 0;

            return new ConversationSummaryResponse(
                conversation.Id,
                conversation.Type.ToString(),
                conversation.Name,
                conversation.CreatedAtUtc,
                conversation.LastMessageAtUtc,
                otherMember?.UserId,
                otherMember?.User.Email,
                currentMember.LastReadAtUtc,
                lastMessage?.Content,
                lastMessage?.CreatedAtUtc,
                lastMessage?.SenderId,
                lastMessage?.Attachments.Count > 0,
                unreadCount);
        }).ToList();
    }
}