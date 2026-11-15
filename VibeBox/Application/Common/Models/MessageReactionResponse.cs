namespace Application.Common.Models;

public sealed record MessageReactionResponse(
    string Emoji,
    int Count,
    bool ReactedByCurrentUser);