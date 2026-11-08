using MediatR;

namespace Application.Features.Conversations.Queries.GetConversationDetails;

public sealed record GetConversationDetailsQuery(Guid ConversationId)
    : IRequest<ConversationDetailsResponse>;