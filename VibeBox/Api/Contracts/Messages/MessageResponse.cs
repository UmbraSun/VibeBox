namespace Api.Contracts.Messages;

public sealed record MessageResponse(
    Guid Id,
    Guid ChannelId,
    Guid SenderId,
    string SenderEmail,
    string Content,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);