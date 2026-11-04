using MediatR;

namespace Application.Features.Conversations.Commands.CreateDirectConversation;

public sealed record CreateDirectConversationCommand(Guid UserId)
    : IRequest<Guid>;