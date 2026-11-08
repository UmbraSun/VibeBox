using MediatR;

namespace Application.Features.Conversations.Commands.LeaveConversation;

public sealed record LeaveConversationCommand(Guid ConversationId) : IRequest<Guid>;