namespace Application.Features.Conversations.Queries.GetMyConversations;

public sealed record ConversationSummaryResponse(
    Guid Id,
    string Type,
    string? Name,
    DateTime CreatedAtUtc,
    DateTime? LastMessageAtUtc,
    Guid? OtherUserId,
    string? OtherUserEmail,
    DateTime? LastReadAtUtc);