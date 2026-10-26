using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Messages.Queries.GetMessageAttachment;

public sealed class GetMessageAttachmentQueryHandler
    : IRequestHandler<GetMessageAttachmentQuery, MessageAttachmentDownload>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IObjectStorageService _objectStorage;

    public GetMessageAttachmentQueryHandler(
        IMessageRepository messageRepository,
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService,
        IObjectStorageService objectStorage)
    {
        _messageRepository = messageRepository;
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
        _objectStorage = objectStorage;
    }

    public async Task<MessageAttachmentDownload> Handle(
        GetMessageAttachmentQuery request,
        CancellationToken cancellationToken)
    {
        var attachment = await _messageRepository.GetAttachmentByIdAsync(
            request.AttachmentId,
            cancellationToken);

        if (attachment is null)
            throw new KeyNotFoundException("Attachment was not found.");

        var member = await _spaceRepository.GetMemberAsync(
            attachment.Message.Channel.SpaceId,
            _currentUserService.UserId,
            cancellationToken);

        if (member is null)
            throw new ForbiddenAccessException("You are not a member of this space.");

        var stream = await _objectStorage.DownloadAsync(
            attachment.StorageKey,
            cancellationToken);

        if (stream is null)
            throw new KeyNotFoundException("Attachment file was not found.");

        return new MessageAttachmentDownload(
            stream,
            attachment.FileName,
            attachment.ContentType);
    }
}
