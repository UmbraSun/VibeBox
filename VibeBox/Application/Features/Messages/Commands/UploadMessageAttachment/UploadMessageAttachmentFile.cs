
namespace Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed record UploadMessageAttachmentFile(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);
