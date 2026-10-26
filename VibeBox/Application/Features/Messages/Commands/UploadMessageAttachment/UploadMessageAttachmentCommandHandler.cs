
using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.Messages.Commands.UploadMessageAttachment;

public sealed class UploadMessageAttachmentCommandHandler
    : IRequestHandler<UploadMessageAttachmentCommand, Guid>
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const long MaxVideoSizeBytes = 100 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, string> AllowedFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm"
        };

    private readonly IMessageRepository _messageRepository;
    private readonly ISpaceRepository _spaceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;
    private readonly IObjectStorageService _objectStorage;

    public UploadMessageAttachmentCommandHandler(
        IMessageRepository messageRepository,
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext context,
        IObjectStorageService objectStorage)
    {
        _messageRepository = messageRepository;
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
        _context = context;
        _objectStorage = objectStorage;
    }

    public async Task<Guid> Handle(
        UploadMessageAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        var channel = await _spaceRepository.GetChannelByIdAsync(
            request.ChannelId,
            cancellationToken);

        if (channel is null)
            throw new KeyNotFoundException("Channel was not found.");

        if (channel.Type != ChannelType.Text)
            throw new ConflictException("Attachments can only be sent to text channels.");

        var member = await _spaceRepository.GetMemberAsync(
            channel.SpaceId,
            _currentUserService.UserId,
            cancellationToken);

        if (member is null)
            throw new ForbiddenAccessException("You are not a member of this space.");

        var extension = Path.GetExtension(request.FileName);

        if (!AllowedFiles.TryGetValue(extension, out var expectedContentType) ||
            !string.Equals(
                request.ContentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Unsupported file type.");

        var isVideo = expectedContentType.StartsWith(
            "video/",
            StringComparison.OrdinalIgnoreCase);

        var maxSize = isVideo ? MaxVideoSizeBytes : MaxImageSizeBytes;

        if (request.SizeBytes <= 0 || request.SizeBytes > maxSize)
            throw new ValidationException(isVideo
                ? "Video size must not exceed 100 MB."
                : "Image size must not exceed 10 MB.");

        var messageId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var storageKey = $"attachments/{channel.Id}/{messageId}/{attachmentId}";

        var message = new Message(
            messageId,
            channel.Id,
            _currentUserService.UserId,
            null,
            DateTime.UtcNow);

        var attachment = new MessageAttachment(
            attachmentId,
            messageId,
            Path.GetFileName(request.FileName),
            expectedContentType,
            request.SizeBytes,
            storageKey,
            DateTime.UtcNow);

        message.AddAttachment(attachment);

        await _objectStorage.UploadAsync(
            storageKey,
            request.Content,
            expectedContentType,
            cancellationToken);

        try
        {
            await _messageRepository.AddAsync(message, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return messageId;
        }
        catch
        {
            try
            {
                await _objectStorage.DeleteAsync(
                    storageKey,
                    CancellationToken.None);
            }
            catch
            {
                // Best-effort cleanup; preserve the original exception.
            }

            throw;
        }
    }
}
