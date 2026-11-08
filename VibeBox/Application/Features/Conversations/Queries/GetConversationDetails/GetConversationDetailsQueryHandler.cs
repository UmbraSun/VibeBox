using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Conversations.Queries.GetConversationDetails;

public sealed class GetConversationDetailsQueryHandler
    : IRequestHandler<GetConversationDetailsQuery, ConversationDetailsResponse>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetConversationDetailsQueryHandler(
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService)
    {
        _conversationRepository = conversationRepository;
        _currentUserService = currentUserService;
    }

    public async Task<ConversationDetailsResponse> Handle(
        GetConversationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var conversation = await _conversationRepository.GetByIdForMemberAsync(
            request.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            throw new KeyNotFoundException("Conversation not found.");

        var members = conversation.Members
            .Select(member => new ConversationMemberResponse(
                member.UserId,
                member.User.Email,
                member.JoinedAtUtc))
            .OrderBy(member => member.JoinedAtUtc)
            .ToList();

        return new ConversationDetailsResponse(
            conversation.Id,
            conversation.Type.ToString(),
            conversation.Name,
            conversation.CreatedAtUtc,
            currentUserId,
            members);
    }
}