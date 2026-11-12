using MediatR;

namespace Application.Features.Conversations.Commands.SendConversationMessage;

public sealed record SendConversationMessageCommand(
    Guid ConversationId,
    string Content,
    Guid? ReplyToMessageId = null) : IRequest<Guid>;