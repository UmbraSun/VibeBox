namespace Application.Common.Models;

public sealed record SendMessageRequest(
    string Content,
    Guid? ReplyToMessageId = null);