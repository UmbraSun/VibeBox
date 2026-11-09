using MediatR;

namespace Application.Features.Conversations.Commands.MarkConversationAsRead;

public sealed record MarkConversationAsReadCommand(
    Guid ConversationId,
    DateTime ReadAtUtc) : IRequest;