
namespace Application.Features.Messages.Queries.GetMessageAttachment;

public sealed record MessageAttachmentDownload(
    Stream Content,
    string FileName,
    string ContentType);
