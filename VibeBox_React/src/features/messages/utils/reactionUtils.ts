
import type { MessageResponse } from "@/api/messagesApi";
import type { MessageReactionChangedEvent } from "@/features/messages/hooks/useMessageHub";

export function applyReactionChange(
    messages: MessageResponse[],
    event: MessageReactionChangedEvent,
    currentUserId: string | null,
): MessageResponse[] {
    return messages.map((message) => {
        if (message.id !== event.messageId) return message;

        const reactions = message.reactions ?? [];
        const existing = reactions.find(
            (reaction) => reaction.emoji === event.emoji,
        );

        const isOwnEvent = Boolean(
            currentUserId && event.userId === currentUserId,
        );

        if (event.added) {
            const updatedReaction = existing
                ? {
                    ...existing,
                    count: existing.count + 1,
                    reactedByCurrentUser:
                        existing.reactedByCurrentUser || isOwnEvent,
                }
                : {
                    emoji: event.emoji,
                    count: 1,
                    reactedByCurrentUser: isOwnEvent,
                };

            return {
                ...message,
                reactions: existing
                    ? reactions.map((reaction) =>
                        reaction.emoji === event.emoji
                            ? updatedReaction
                            : reaction,
                    )
                    : [...reactions, updatedReaction],
            };
        }

        if (!existing) return message;

        const updatedReactions =
            existing.count <= 1
                ? reactions.filter(
                    (reaction) => reaction.emoji !== event.emoji,
                )
                : reactions.map((reaction) =>
                    reaction.emoji === event.emoji
                        ? {
                            ...reaction,
                            count: reaction.count - 1,
                            reactedByCurrentUser: isOwnEvent
                                ? false
                                : reaction.reactedByCurrentUser,
                        }
                        : reaction,
                );

        return {
            ...message,
            reactions: updatedReactions,
        };
    });
}
