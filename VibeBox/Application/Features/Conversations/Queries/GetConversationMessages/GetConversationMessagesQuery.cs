using Application.Common.Models;
using MediatR;

namespace Application.Features.Conversations.Queries.GetConversationMessages;

public sealed record GetConversationMessagesQuery(
    Guid ConversationId,
    Guid? BeforeMessageId,
    int Limit = 50)
    : IRequest<IReadOnlyList<MessageResponse>>;