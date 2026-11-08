using MediatR;

namespace Application.Features.Conversations.Commands.RenameConversation;

public sealed record RenameConversationCommand(
    Guid ConversationId,
    string Name) : IRequest;