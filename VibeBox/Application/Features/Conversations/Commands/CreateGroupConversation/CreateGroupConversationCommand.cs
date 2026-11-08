using MediatR;

namespace Application.Features.Conversations.Commands.CreateGroupConversation;

public sealed record CreateGroupConversationCommand(
    string Name,
    IReadOnlyList<Guid> UserIds) : IRequest<Guid>;