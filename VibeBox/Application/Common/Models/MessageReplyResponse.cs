namespace Application.Common.Models;

public sealed record MessageReplyResponse(
    Guid Id,
    Guid SenderId,
    string SenderEmail,
    string? Content);