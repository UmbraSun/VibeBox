import { useCallback, useEffect, useRef, useState } from "react";
import {
    conversationsApi,
    type ConversationDetailsResponse,
    type ConversationSummaryResponse,
} from "@/api/conversationsApi";
import { getErrorMessage } from "@/features/messages/utils/errorUtils";

export function useConversations() {
    const [conversations, setConversations] = useState<
        ConversationSummaryResponse[]
    >([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isCreating, setIsCreating] = useState(false);
    const [isUpdating, setIsUpdating] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const loadPromiseRef = useRef<Promise<void> | null>(null);
    const reloadRequestedRef = useRef(false);

    const loadConversations = useCallback((): Promise<void> => {
        if (loadPromiseRef.current) {
            reloadRequestedRef.current = true;
            return loadPromiseRef.current;
        }

        const load = async (): Promise<void> => {
            do {
                reloadRequestedRef.current = false;

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
            } while (reloadRequestedRef.current);
        };

        const promise = load().finally(() => {
            if (loadPromiseRef.current === promise) {
                loadPromiseRef.current = null;
            }
        });

        loadPromiseRef.current = promise;

        return promise;
    }, []);

    const createDirect = useCallback(
        async (userId: string): Promise<string> => {
            if (!userId.trim() || isCreating) {
                throw new Error("A valid user ID is required.");
            }

            setIsCreating(true);
            setError(null);

            try {
                const conversationId =
                    await conversationsApi.createDirect(userId.trim());

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

    const createGroup = useCallback(
        async (name: string, userIds: string[]): Promise<string> => {
            if (isCreating) {
                throw new Error("Another conversation is being created.");
            }

            setIsCreating(true);
            setError(null);

            try {
                const conversationId = await conversationsApi.createGroup(
                    name,
                    userIds,
                );

                await loadConversations();

                return conversationId;
            } catch (requestError: unknown) {
                const message = getErrorMessage(
                    requestError,
                    "Unable to create group.",
                );

                setError(message);
                throw new Error(message);
            } finally {
                setIsCreating(false);
            }
        },
        [isCreating, loadConversations],
    );

    const loadDetails = useCallback(
        async (conversationId: string): Promise<ConversationDetailsResponse> => {
            try {
                return await conversationsApi.getDetails(conversationId);
            } catch (requestError: unknown) {
                const message = getErrorMessage(
                    requestError,
                    "Unable to load conversation details.",
                );

                setError(message);
                throw new Error(message);
            }
        },
        [],
    );

    const executeUpdate = useCallback(
        async (
            action: () => Promise<void>,
            fallbackMessage: string,
        ): Promise<void> => {
            if (isUpdating) {
                throw new Error("Another conversation update is in progress.");
            }

            setIsUpdating(true);
            setError(null);

            try {
                await action();
                await loadConversations();
            } catch (requestError: unknown) {
                const message = getErrorMessage(
                    requestError,
                    fallbackMessage,
                );

                setError(message);
                throw new Error(message);
            } finally {
                setIsUpdating(false);
            }
        },
        [isUpdating, loadConversations],
    );

    const addMembers = useCallback(
        async (conversationId: string, userIds: string[]) => {
            await executeUpdate(
                () => conversationsApi.addMembers(conversationId, userIds),
                "Unable to add group members.",
            );
        },
        [executeUpdate],
    );

    const removeMember = useCallback(
        async (conversationId: string, userId: string) => {
            await executeUpdate(
                () => conversationsApi.removeMember(conversationId, userId),
                "Unable to remove group member.",
            );
        },
        [executeUpdate],
    );

    const leaveConversation = useCallback(
        async (conversationId: string) => {
            await executeUpdate(
                () => conversationsApi.leaveConversation(conversationId),
                "Unable to leave group.",
            );
        },
        [executeUpdate],
    );

    const renameConversation = useCallback(
        async (conversationId: string, name: string) => {
            await executeUpdate(
                () => conversationsApi.renameConversation(conversationId, name),
                "Unable to rename group.",
            );
        },
        [executeUpdate],
    );

    useEffect(() => {
        void loadConversations();
    }, [loadConversations]);

    return {
        conversations,
        isLoading,
        isCreating,
        isUpdating,
        error,
        loadConversations,
        loadDetails,
        createDirect,
        createGroup,
        addMembers,
        removeMember,
        leaveConversation,
        renameConversation,
    };
}
