using MediatR;

namespace Application.Features.Conversations.Commands.AddConversationMembers;

public sealed record AddConversationMembersCommand(
    Guid ConversationId,
    IReadOnlyList<Guid> UserIds) : IRequest;