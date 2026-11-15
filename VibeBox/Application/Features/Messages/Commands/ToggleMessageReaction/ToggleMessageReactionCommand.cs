using MediatR;

namespace Application.Features.Messages.Commands.ToggleMessageReaction;

public sealed record ToggleMessageReactionCommand(
    Guid? ChannelId,
    Guid? ConversationId,
    Guid MessageId,
    string Emoji) : IRequest<bool>;