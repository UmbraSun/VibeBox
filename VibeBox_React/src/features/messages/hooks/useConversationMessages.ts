import { useCallback, useEffect, useState } from "react";
import type { MessageResponse } from "@/api/messagesApi";
import { messageService } from "@/features/messages/services/messageService";
import { useMessageHub } from "./useMessageHub";

const PAGE_SIZE = 50;

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

export function useConversationMessages(conversationId: string) {
    const [messages, setMessages] = useState<MessageResponse[]>([]);
    const [connectionStatus, setConnectionStatus] =
        useState<ConnectionStatus>("disconnected");
    const [isLoading, setIsLoading] = useState(true);
    const [isLoadingOlder, setIsLoadingOlder] = useState(false);
    const [hasMoreMessages, setHasMoreMessages] = useState(true);
    const [isSending, setIsSending] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const loadMessages = useCallback(async () => {
        try {
            const result = await messageService.getConversationMessages(
                conversationId,
                PAGE_SIZE,
            );

            setMessages(result);
            setHasMoreMessages(result.length === PAGE_SIZE);
            setError(null);
        } catch (requestError: unknown) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to load messages.",
            );
        } finally {
            setIsLoading(false);
        }
    }, [conversationId]);

    const refreshLatestMessages = useCallback(async () => {
        try {
            const result = await messageService.getConversationMessages(
                conversationId,
                PAGE_SIZE,
            );

            setMessages((current) => mergeMessages(current, result));
            setError(null);
        } catch (requestError: unknown) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to refresh messages.",
            );
        }
    }, [conversationId]);

    const loadOlderMessages = useCallback(async () => {
        if (
            isLoading ||
            isLoadingOlder ||
            !hasMoreMessages ||
            messages.length === 0
        ) {
            return [];
        }

        setIsLoadingOlder(true);

        try {
            const result = await messageService.getConversationMessages(
                conversationId,
                PAGE_SIZE,
                messages[0].id,
            );

            setMessages((current) => {
                const existingIds = new Set(
                    current.map((message) => message.id),
                );

                const olderMessages = result.filter(
                    (message) => !existingIds.has(message.id),
                );

                return [...olderMessages, ...current];
            });

            setHasMoreMessages(result.length === PAGE_SIZE);
            setError(null);

            return result;
        } catch (requestError: unknown) {
            setError(
                requestError instanceof Error
                    ? requestError.message
                    : "Unable to load older messages.",
            );

            return [];
        } finally {
            setIsLoadingOlder(false);
        }
    }, [
        conversationId,
        hasMoreMessages,
        isLoading,
        isLoadingOlder,
        messages,
    ]);

    const sendMessage = useCallback(
        async (content: string) => {
            const normalizedContent = content.trim();

            if (!normalizedContent || isSending) return;

            setIsSending(true);
            setError(null);

            try {
                await messageService.sendConversationMessage(
                    conversationId,
                    normalizedContent,
                );

                await refreshLatestMessages();
            } catch (requestError: unknown) {
                setError(
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to send message.",
                );

                throw requestError;
            } finally {
                setIsSending(false);
            }
        },
        [conversationId, isSending, refreshLatestMessages],
    );

    const sendAttachments = useCallback(
        async (files: File[], content?: string) => {
            if (isSending || files.length === 0) return;

            setIsSending(true);
            setError(null);

            try {
                await messageService.sendConversationAttachments(
                    conversationId,
                    files,
                    content,
                );

                await refreshLatestMessages();
            } catch (requestError: unknown) {
                setError(
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to upload attachments.",
                );

                throw requestError;
            } finally {
                setIsSending(false);
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

                setMessages((current) =>
                    current.filter((message) => message.id !== messageId),
                );
            } catch (requestError: unknown) {
                setError(
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to delete message.",
                );

                throw requestError;
            }
        },
        [conversationId],
    );

    const handleMessageCreated = useCallback(() => {
        void refreshLatestMessages();
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
        setMessages([]);
        setIsLoading(true);
        setIsLoadingOlder(false);
        setHasMoreMessages(true);
        setError(null);

        void loadMessages();
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