using MediatR;

namespace Application.Features.Messages.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ChannelId,
    string Content) : IRequest<Guid>;