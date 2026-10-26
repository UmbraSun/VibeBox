using MediatR;

namespace Application.Features.Messages.Queries.GetMessageAttachment;

public sealed record GetMessageAttachmentQuery(Guid AttachmentId)
    : IRequest<MessageAttachmentDownload>;
