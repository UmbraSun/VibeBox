using MediatR;

namespace Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed record UploadMessageAttachmentCommand(
    Guid ChannelId,
    string? Content,
    IReadOnlyList<UploadMessageAttachmentFile> Files)
    : IRequest<Guid>;