
using MediatR;

namespace Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed record UploadMessageAttachmentCommand(
    Guid ChannelId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content) : IRequest<Guid>;
