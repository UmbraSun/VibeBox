using MediatR;

namespace Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed record UploadMessageAttachmentCommand(
    Guid? ChannelId,
    Guid? ConversationId,
    string? Content,
    IReadOnlyList<UploadMessageAttachmentFile> Files)
    : IRequest<Guid>;