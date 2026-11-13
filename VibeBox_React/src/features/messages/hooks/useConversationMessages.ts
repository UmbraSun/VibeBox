
import {
    useCallback,
    useEffect,
    useRef,
    useState,
} from "react";
import type { MessageResponse } from "@/api/messagesApi";
import { messageService } from "@/features/messages/services/messageService";
import { useMessageHub } from "./useMessageHub";

const PAGE_SIZE = 50;
const HUB_REFRESH_DELAY = 100;
const RECENT_REFRESH_WINDOW = 500;

type ConnectionStatus =
    | "disconnected"
    | "connecting"
    | "connected"
    | "error";

function mergeMessages(
    current: MessageResponse[],
    incoming: MessageResponse[],
): MessageResponse[] {
    const messagesById = new Map(
        current.map((message) => [message.id, message]),
    );

    for (const message of incoming) {
        messagesById.set(message.id, message);
    }

    return [...messagesById.values()].sort((a, b) => {
        const dateDifference =
            new Date(a.createdAtUtc).getTime() -
            new Date(b.createdAtUtc).getTime();

        return dateDifference || a.id.localeCompare(b.id);
    });
}

function getErrorMessage(
    error: unknown,
    fallback: string,
): string {
    return error instanceof Error ? error.message : fallback;
}

export function useConversationMessages(conversationId: string) {
    const [messages, setMessages] = useState<MessageResponse[]>([]);
    const [connectionStatus, setConnectionStatus] =
        useState<ConnectionStatus>("disconnected");
    const [isLoading, setIsLoading] = useState(true);
    const [isLoadingOlder, setIsLoadingOlder] = useState(false);
    const [hasMoreMessages, setHasMoreMessages] = useState(true);
    const [isSending, setIsSending] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const activeConversationIdRef = useRef(conversationId);
    activeConversationIdRef.current = conversationId;

    const requestVersionRef = useRef(0);

    const olderRequestRef = useRef<{
        conversationId: string;
        requestVersion: number;
    } | null>(null);

    const refreshPromiseRef = useRef<{
        conversationId: string;
        requestVersion: number;
        promise: Promise<void>;
    } | null>(null);

    const hubRefreshTimerRef =
        useRef<ReturnType<typeof setTimeout> | null>(null);

    const lastRefreshAtRef = useRef(0);

    const loadMessages = useCallback(async () => {
        const requestVersion = requestVersionRef.current;

        try {
            const result = await messageService.getConversationMessages(
                conversationId,
                PAGE_SIZE,
            );

            if (
                activeConversationIdRef.current !== conversationId ||
                requestVersionRef.current !== requestVersion
            ) {
                return;
            }

            setMessages(result);
            setHasMoreMessages(result.length === PAGE_SIZE);
            setError(null);
        } catch (requestError: unknown) {
            if (
                activeConversationIdRef.current === conversationId &&
                requestVersionRef.current === requestVersion
            ) {
                setError(
                    getErrorMessage(
                        requestError,
                        "Unable to load messages.",
                    ),
                );
            }
        } finally {
            if (
                activeConversationIdRef.current === conversationId &&
                requestVersionRef.current === requestVersion
            ) {
                setIsLoading(false);
            }
        }
    }, [conversationId]);

    const refreshLatestMessages = useCallback((): Promise<void> => {
        const requestVersion = requestVersionRef.current;

        if (activeConversationIdRef.current !== conversationId) {
            return Promise.resolve();
        }

        const existingRequest = refreshPromiseRef.current;

        if (
            existingRequest?.conversationId === conversationId &&
            existingRequest.requestVersion === requestVersion
        ) {
            return existingRequest.promise;
        }

        lastRefreshAtRef.current = Date.now();

        const refresh = async (): Promise<void> => {
            try {
                const result = await messageService.getConversationMessages(
                    conversationId,
                    PAGE_SIZE,
                );

                if (
                    activeConversationIdRef.current !== conversationId ||
                    requestVersionRef.current !== requestVersion
                ) {
                    return;
                }

                setMessages((current) => mergeMessages(current, result));
                setError(null);
            } catch (requestError: unknown) {
                if (
                    activeConversationIdRef.current === conversationId &&
                    requestVersionRef.current === requestVersion
                ) {
                    setError(
                        getErrorMessage(
                            requestError,
                            "Unable to refresh messages.",
                        ),
                    );
                }
            }
        };

        const promise = refresh().finally(() => {
            if (refreshPromiseRef.current?.promise === promise) {
                refreshPromiseRef.current = null;
            }
        });

        refreshPromiseRef.current = {
            conversationId,
            requestVersion,
            promise,
        };

        return promise;
    }, [conversationId]);

    const loadOlderMessages = useCallback(async () => {
        if (
            isLoading ||
            olderRequestRef.current ||
            !hasMoreMessages ||
            messages.length === 0
        ) {
            return [];
        }

        const requestVersion = requestVersionRef.current;
        const oldestMessageId = messages[0].id;

        const request = {
            conversationId,
            requestVersion,
        };

        olderRequestRef.current = request;
        setIsLoadingOlder(true);

        try {
            const result = await messageService.getConversationMessages(
                conversationId,
                PAGE_SIZE,
                oldestMessageId,
            );

            if (
                activeConversationIdRef.current !== conversationId ||
                requestVersionRef.current !== requestVersion
            ) {
                return [];
            }

            setMessages((current) => mergeMessages(current, result));
            setHasMoreMessages(result.length === PAGE_SIZE);
            setError(null);

            return result;
        } catch (requestError: unknown) {
            if (
                activeConversationIdRef.current === conversationId &&
                requestVersionRef.current === requestVersion
            ) {
                setError(
                    getErrorMessage(
                        requestError,
                        "Unable to load older messages.",
                    ),
                );
            }

            return [];
        } finally {
            if (olderRequestRef.current === request) {
                olderRequestRef.current = null;

                if (
                    activeConversationIdRef.current === conversationId &&
                    requestVersionRef.current === requestVersion
                ) {
                    setIsLoadingOlder(false);
                }
            }
        }
    }, [
        conversationId,
        hasMoreMessages,
        isLoading,
        messages,
    ]);

    const sendMessage = useCallback(
        async (
            content: string,
            replyToMessageId?: string | null,
        ) => {
            const normalizedContent = content.trim();

            if (!normalizedContent || isSending) return;

            setIsSending(true);
            setError(null);

            try {
                await messageService.sendConversationMessage(
                    conversationId,
                    normalizedContent,
                    replyToMessageId,
                );

                await refreshLatestMessages();
            } catch (requestError: unknown) {
                if (activeConversationIdRef.current === conversationId) {
                    setError(
                        getErrorMessage(
                            requestError,
                            "Unable to send message.",
                        ),
                    );
                }

                throw requestError;
            } finally {
                if (activeConversationIdRef.current === conversationId) {
                    setIsSending(false);
                }
            }
        },
        [conversationId, isSending, refreshLatestMessages],
    );

    const sendAttachments = useCallback(
        async (
            files: File[],
            content?: string,
            replyToMessageId?: string | null,
        ) => {
            if (isSending || files.length === 0) return;

            setIsSending(true);
            setError(null);

            try {
                await messageService.sendConversationAttachments(
                    conversationId,
                    files,
                    content,
                    replyToMessageId,
                );

                await refreshLatestMessages();
            } catch (requestError: unknown) {
                if (activeConversationIdRef.current === conversationId) {
                    setError(
                        getErrorMessage(
                            requestError,
                            "Unable to upload attachments.",
                        ),
                    );
                }

                throw requestError;
            } finally {
                if (activeConversationIdRef.current === conversationId) {
                    setIsSending(false);
                }
            }
        },
        [conversationId, isSending, refreshLatestMessages],
    );

    const deleteMessage = useCallback(
        async (messageId: string) => {
            setError(null);

            try {
                await messageService.deleteConversationMessage(
                    conversationId,
                    messageId,
                );

                if (activeConversationIdRef.current !== conversationId) {
                    return;
                }

                setMessages((current) =>
                    current.filter((message) => message.id !== messageId),
                );
            } catch (requestError: unknown) {
                if (activeConversationIdRef.current === conversationId) {
                    setError(
                        getErrorMessage(
                            requestError,
                            "Unable to delete message.",
                        ),
                    );
                }

                throw requestError;
            }
        },
        [conversationId],
    );

    const handleMessageCreated = useCallback(() => {
        if (
            Date.now() - lastRefreshAtRef.current <
            RECENT_REFRESH_WINDOW
        ) {
            return;
        }

        if (hubRefreshTimerRef.current) {
            clearTimeout(hubRefreshTimerRef.current);
        }

        hubRefreshTimerRef.current = setTimeout(() => {
            hubRefreshTimerRef.current = null;
            void refreshLatestMessages();
        }, HUB_REFRESH_DELAY);
    }, [refreshLatestMessages]);

    const handleMessageDeleted = useCallback((messageId: string) => {
        setMessages((current) =>
            current.filter((message) => message.id !== messageId),
        );
    }, []);

    const handleConnected = useCallback(() => {
        void refreshLatestMessages();
    }, [refreshLatestMessages]);

    const handleStatusChange = useCallback((status: ConnectionStatus) => {
        setConnectionStatus(status);
    }, []);

    const handleHubError = useCallback((message: string) => {
        setError(message);
        setIsLoading(false);
    }, []);

    useEffect(() => {
        requestVersionRef.current += 1;

        if (hubRefreshTimerRef.current) {
            clearTimeout(hubRefreshTimerRef.current);
            hubRefreshTimerRef.current = null;
        }

        olderRequestRef.current = null;
        refreshPromiseRef.current = null;
        lastRefreshAtRef.current = 0;

        setMessages([]);
        setIsLoading(true);
        setIsLoadingOlder(false);
        setHasMoreMessages(true);
        setIsSending(false);
        setError(null);

        void loadMessages();

        return () => {
            requestVersionRef.current += 1;

            if (hubRefreshTimerRef.current) {
                clearTimeout(hubRefreshTimerRef.current);
                hubRefreshTimerRef.current = null;
            }
        };
    }, [conversationId, loadMessages]);

    useMessageHub({
        conversationId,
        onMessageCreated: handleMessageCreated,
        onMessageDeleted: handleMessageDeleted,
        onConnected: handleConnected,
        onStatusChange: handleStatusChange,
        onError: handleHubError,
    });

    return {
        messages,
        connectionStatus,
        isLoading,
        isLoadingOlder,
        hasMoreMessages,
        isSending,
        error,
        sendMessage,
        sendAttachments,
        deleteMessage,
        loadOlderMessages,
        reloadMessages: refreshLatestMessages,
    };
}
