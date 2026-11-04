using MediatR;

namespace Application.Features.Conversations.Queries.GetMyConversations;

public sealed record GetMyConversationsQuery
    : IRequest<IReadOnlyList<ConversationSummaryResponse>>;