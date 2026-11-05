import { useCallback, useEffect, useState } from "react";
import {
    conversationsApi,
    type ConversationSummaryResponse,
} from "@/api/conversationsApi";
import { getErrorMessage } from "@/features/messages/utils/errorUtils";

export function useConversations() {
    const [conversations, setConversations] = useState<
        ConversationSummaryResponse[]
    >([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isCreating, setIsCreating] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const loadConversations = useCallback(async () => {
        try {
            const result = await conversationsApi.getMyConversations();

            setConversations(result);
            setError(null);
        } catch (requestError: unknown) {
            setError(
                getErrorMessage(
                    requestError,
                    "Unable to load conversations.",
                ),
            );
        } finally {
            setIsLoading(false);
        }
    }, []);

    const createDirect = useCallback(
        async (userId: string): Promise<string> => {
            const normalizedUserId = userId.trim();

            if (!normalizedUserId || isCreating) {
                throw new Error("A valid user ID is required.");
            }

            setIsCreating(true);
            setError(null);

            try {
                const conversationId =
                    await conversationsApi.createDirect(normalizedUserId);

                await loadConversations();

                return conversationId;
            } catch (requestError: unknown) {
                const message = getErrorMessage(
                    requestError,
                    "Unable to create conversation.",
                );

                setError(message);
                throw new Error(message);
            } finally {
                setIsCreating(false);
            }
        },
        [isCreating, loadConversations],
    );

    useEffect(() => {
        void loadConversations();
    }, [loadConversations]);

    return {
        conversations,
        isLoading,
        isCreating,
        error,
        loadConversations,
        createDirect,
    };
}