using MediatR;

namespace Application.Features.Conversations.Commands.RemoveConversationMember;

public sealed record RemoveConversationMemberCommand(
    Guid ConversationId,
    Guid UserId) : IRequest<Guid>;