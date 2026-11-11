using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.Messages.Queries.GetMessageAttachment;

public sealed class GetMessageAttachmentQueryHandler
    : IRequestHandler<GetMessageAttachmentQuery, MessageAttachmentDownload>
{
    private readonly IMessageRepository _messageRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IObjectStorageService _objectStorage;

    public GetMessageAttachmentQueryHandler(
        IMessageRepository messageRepository,
        ISpaceRepository spaceRepository,
        IConversationRepository conversationRepository,
        ICurrentUserService currentUserService,
        IObjectStorageService objectStorage)
    {
        _messageRepository = messageRepository;
        _spaceRepository = spaceRepository;
        _conversationRepository = conversationRepository;
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

        var message = attachment.Message;
        var userId = _currentUserService.UserId;

        if (message.ChannelId.HasValue)
        {
            var channel = message.Channel
                ?? throw new InvalidOperationException(
                    "Attachment message channel was not loaded.");

            var member = await _spaceRepository.GetMemberAsync(
                channel.SpaceId,
                userId,
                cancellationToken);

            if (member is null)
                throw new ForbiddenAccessException(
                    "You are not a member of this space.");
        }
        else if (message.ConversationId.HasValue)
        {
            var conversation = await _conversationRepository.GetByIdForMemberAsync(
                message.ConversationId.Value,
                userId,
                cancellationToken);

            if (conversation is null)
                throw new KeyNotFoundException("Conversation not found.");
        }
        else
        {
            throw new InvalidOperationException(
                "Attachment message has no associated channel or conversation.");
        }

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