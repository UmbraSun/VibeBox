namespace Application.Features.Conversations.Queries.GetConversationDetails;

public sealed record ConversationDetailsResponse(
    Guid Id,
    string Type,
    string? Name,
    DateTime CreatedAtUtc,
    Guid CurrentUserId,
    IReadOnlyList<ConversationMemberResponse> Members);

public sealed record ConversationMemberResponse(
    Guid UserId,
    string Email,
    DateTime JoinedAtUtc);