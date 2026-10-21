using Domain.Entities;
using MediatR;

namespace Application.Features.Messages.Queries.GetChannelMessages;

public sealed record GetChannelMessagesQuery(
    Guid ChannelId,
    Guid? BeforeMessageId,
    int Limit = 50) : IRequest<IReadOnlyList<Message>>;