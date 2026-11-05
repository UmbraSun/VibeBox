using MediatR;

namespace Application.Features.Messages.Commands.DeleteMessage;

public sealed record DeleteMessageCommand(
    Guid? ChannelId,
    Guid? ConversationId,
    Guid MessageId) : IRequest;