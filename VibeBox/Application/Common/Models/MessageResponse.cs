namespace Application.Common.Models;

public sealed record MessageResponse(
    Guid Id,
    Guid? ChannelId,
    Guid SenderId,
    string SenderEmail,
    string? Content,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<MessageAttachmentResponse> Attachments,
    Guid? ReplyToMessageId,
    MessageReplyResponse? ReplyToMessage);