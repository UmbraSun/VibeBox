
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
    private readonly IConversationRepository _conversationRepository;

    public UploadMessageAttachmentCommandHandler(
        IMessageRepository messageRepository,
        ISpaceRepository spaceRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext context,
        IObjectStorageService objectStorage,
        IConversationRepository conversationRepository)
    {
        _messageRepository = messageRepository;
        _spaceRepository = spaceRepository;
        _currentUserService = currentUserService;
        _context = context;
        _objectStorage = objectStorage;
        _conversationRepository = conversationRepository;
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

        if (request.ChannelId.HasValue == request.ConversationId.HasValue)
        {
            throw new ValidationException(
                "Exactly one of ChannelId or ConversationId must be specified.");
        }

        var currentUserId = _currentUserService.UserId;

        Channel? channel = null;
        Conversation? conversation = null;

        if (request.ChannelId.HasValue)
        {
            channel = await _spaceRepository.GetChannelByIdAsync(
                request.ChannelId.Value,
                cancellationToken);

            if (channel is null)
                throw new KeyNotFoundException("Channel was not found.");

            if (channel.Type != ChannelType.Text)
                throw new ConflictException(
                    "Attachments can only be sent to text channels.");

            var member = await _spaceRepository.GetMemberAsync(
                channel.SpaceId,
                currentUserId,
                cancellationToken);

            if (member is null)
                throw new ForbiddenAccessException(
                    "You are not a member of this space.");
        }
        else
        {
            conversation = await _conversationRepository.GetByIdForMemberAsync(
                request.ConversationId!.Value,
                currentUserId,
                cancellationToken);

            if (conversation is null)
                throw new KeyNotFoundException("Conversation was not found.");
        }

        if (request.Files.Count == 0 || request.Files.Count > MaxFilesCount)
            throw new ValidationException(
                $"You can upload between 1 and {MaxFilesCount} files.");

        Message? replyToMessage = null;

        if (request.ReplyToMessageId.HasValue)
        {
            replyToMessage = await _messageRepository.GetByIdAsync(
                request.ReplyToMessageId.Value,
                cancellationToken);

            if (replyToMessage is null)
                throw new KeyNotFoundException("Reply message was not found.");

            if (channel is not null &&
                replyToMessage.ChannelId != channel.Id)
            {
                throw new ConflictException(
                    "Reply message must belong to the same channel.");
            }

            if (conversation is not null &&
                replyToMessage.ConversationId != conversation.Id)
            {
                throw new ConflictException(
                    "Reply message must belong to the same conversation.");
            }
        }

        long totalSize = 0;

        foreach (var file in request.Files)
        {
            if (file.Content is null ||
                !file.Content.CanRead ||
                !file.Content.CanSeek)
            {
                throw new ValidationException(
                    $"File stream is invalid: {file.FileName}.");
            }

            file.Content.Position = 0;

            var actualSize = file.Content.Length;

            if (actualSize <= 0)
                throw new ValidationException(
                    "Empty files cannot be uploaded.");

            if (file.SizeBytes != actualSize)
                throw new ValidationException(
                    $"File size does not match its content: {file.FileName}.");

            if (actualSize > MaxTotalSizeBytes - totalSize)
                throw new ValidationException(
                    "Total attachment size must not exceed 150 MB.");

            totalSize += actualSize;

            var safeFileName = NormalizeFileName(file.FileName);
            var extension = Path.GetExtension(safeFileName);

            if (!AllowedFiles.TryGetValue(
                    extension,
                    out var expectedContentType) ||
                !string.Equals(
                    file.ContentType,
                    expectedContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(
                    $"Unsupported file type: {safeFileName}.");
            }

            var isVideo = expectedContentType.StartsWith(
                "video/",
                StringComparison.OrdinalIgnoreCase);

            var maxSize = isVideo
                ? MaxVideoSizeBytes
                : MaxImageSizeBytes;

            if (actualSize > maxSize)
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
                    $"File content does not match its declared format: {safeFileName}.");
            }

            file.Content.Position = 0;
        }

        var messageId = Guid.NewGuid();
        var createdAtUtc = DateTime.UtcNow;

        var message = channel is not null
            ? new Message(
                messageId,
                channel.Id,
                currentUserId,
                content,
                createdAtUtc,
                request.ReplyToMessageId)
            : Message.CreateForConversation(
                messageId,
                conversation!.Id,
                currentUserId,
                content,
                createdAtUtc,
                request.ReplyToMessageId);

        conversation?.UpdateLastMessageAt(createdAtUtc);

        var storageKeys = new List<string>(request.Files.Count);

        foreach (var file in request.Files)
        {
            var attachmentId = Guid.NewGuid();
            var storageKey = channel is not null
                ? $"attachments/{channel.Id}/{messageId}/{attachmentId}"
                : $"attachments/conversations/{conversation!.Id}/{messageId}/{attachmentId}";

            var attachment = new MessageAttachment(
                attachmentId,
                messageId,
                NormalizeFileName(file.FileName),
                AllowedFiles[Path.GetExtension(NormalizeFileName(file.FileName))],
                file.Content.Length,
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
                    AllowedFiles[Path.GetExtension(NormalizeFileName(file.FileName))],
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

    private static string NormalizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ValidationException("File name is required.");

        var normalized = fileName.Replace('\\', '/');
        var safeFileName = Path.GetFileName(normalized).Trim();

        if (string.IsNullOrWhiteSpace(safeFileName) ||
            safeFileName is "." or "..")
        {
            throw new ValidationException("Invalid file name.");
        }

        return safeFileName;
    }
}
