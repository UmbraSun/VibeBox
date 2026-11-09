namespace Api.Contracts.Conversations;

public sealed record MarkConversationAsReadRequest(DateTime ReadAtUtc);