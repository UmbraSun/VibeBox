using MediatR;

namespace Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed record UploadMessageAttachmentCommand(
    Guid? ChannelId,
    Guid? ConversationId,
    string? Content,
    Guid? ReplyToMessageId,
    IReadOnlyList<UploadMessageAttachmentFile> Files)
    : IRequest<Guid>;