
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
    private const int MaxFilesCount = 5;
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private const long MaxVideoSizeBytes = 100 * 1024 * 1024;
    private const long MaxTotalSizeBytes = 150 * 1024 * 1024;

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
        var content = string.IsNullOrWhiteSpace(request.Content)
            ? null
            : request.Content.Trim();

        if (content?.Length > 4000)
            throw new ValidationException(
                "Message content must not exceed 4000 characters.");

        var channel = await _spaceRepository.GetChannelByIdAsync(
            request.ChannelId,
            cancellationToken);

        if (channel is null)
            throw new KeyNotFoundException("Channel was not found.");

        if (channel.Type != ChannelType.Text)
            throw new ConflictException(
                "Attachments can only be sent to text channels.");

        var member = await _spaceRepository.GetMemberAsync(
            channel.SpaceId,
            _currentUserService.UserId,
            cancellationToken);

        if (member is null)
            throw new ForbiddenAccessException(
                "You are not a member of this space.");

        if (request.Files.Count == 0 || request.Files.Count > MaxFilesCount)
            throw new ValidationException(
                $"You can upload between 1 and {MaxFilesCount} files.");

        long totalSize = 0;

        foreach (var file in request.Files)
        {
            if (file.SizeBytes <= 0)
                throw new ValidationException(
                    "Empty files cannot be uploaded.");

            if (file.SizeBytes > MaxTotalSizeBytes - totalSize)
                throw new ValidationException(
                    "Total attachment size must not exceed 150 MB.");

            totalSize += file.SizeBytes;

            var extension = Path.GetExtension(file.FileName);

            if (!AllowedFiles.TryGetValue(
                    extension,
                    out var expectedContentType) ||
                !string.Equals(
                    file.ContentType,
                    expectedContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(
                    $"Unsupported file type: {file.FileName}.");
            }

            var isVideo = expectedContentType.StartsWith(
                "video/",
                StringComparison.OrdinalIgnoreCase);

            var maxSize = isVideo
                ? MaxVideoSizeBytes
                : MaxImageSizeBytes;

            if (file.SizeBytes > maxSize)
            {
                throw new ValidationException(isVideo
                    ? "Video size must not exceed 100 MB."
                    : "Image size must not exceed 10 MB.");
            }

            if (!await HasValidFileSignatureAsync(
                    file.Content,
                    expectedContentType,
                    cancellationToken))
            {
                throw new ValidationException(
                    $"File content does not match its declared format: {file.FileName}.");
            }
        }

        var messageId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;

        var message = new Message(
            messageId,
            channel.Id,
            _currentUserService.UserId,
            content,
            createdAtUtc);

        var storageKeys = new List<string>(request.Files.Count);

        foreach (var file in request.Files)
        {
            var attachmentId = Guid.NewGuid();
            var storageKey =
                $"attachments/{channel.Id}/{messageId}/{attachmentId}";

            var attachment = new MessageAttachment(
                attachmentId,
                messageId,
                Path.GetFileName(file.FileName),
                file.ContentType,
                file.SizeBytes,
                storageKey,
                createdAtUtc);

            message.AddAttachment(attachment);

            storageKeys.Add(storageKey);
        }

        try
        {
            for (var index = 0; index < request.Files.Count; index++)
            {
                var file = request.Files[index];

                await _objectStorage.UploadAsync(
                    storageKeys[index],
                    file.Content,
                    file.ContentType,
                    cancellationToken);
            }

            await _messageRepository.AddAsync(
                message,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return messageId;
        }
        catch
        {
            foreach (var storageKey in storageKeys)
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
            }

            throw;
        }
    }

    private static async Task<bool> HasValidFileSignatureAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException(
                "File stream must support seeking.");

        var originalPosition = stream.Position;

        try
        {
            var header = new byte[12];
            var bytesRead = 0;

            while (bytesRead < header.Length)
            {
                var read = await stream.ReadAsync(
                    header.AsMemory(bytesRead),
                    cancellationToken);

                if (read == 0)
                    break;

                bytesRead += read;
            }

            return contentType switch
            {
                "image/jpeg" =>
                    bytesRead >= 3 &&
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                "image/png" =>
                    bytesRead >= 8 &&
                    header.AsSpan(0, 8).SequenceEqual(
                        new byte[]
                        {
                            0x89, 0x50, 0x4E, 0x47,
                            0x0D, 0x0A, 0x1A, 0x0A
                        }),

                "image/gif" =>
                    bytesRead >= 6 &&
                    (header.AsSpan(0, 6).SequenceEqual("GIF87a"u8) ||
                     header.AsSpan(0, 6).SequenceEqual("GIF89a"u8)),

                "image/webp" =>
                    bytesRead >= 12 &&
                    header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                    header.AsSpan(8, 4).SequenceEqual("WEBP"u8),

                "video/mp4" =>
                    bytesRead >= 8 &&
                    header.AsSpan(4, 4).SequenceEqual("ftyp"u8),

                "video/webm" =>
                    bytesRead >= 4 &&
                    header.AsSpan(0, 4).SequenceEqual(
                        new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }),

                _ => false
            };
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
}
