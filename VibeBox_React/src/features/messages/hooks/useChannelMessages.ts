import { useCallback, useEffect, useState } from "react";
import type { MessageResponse } from "@/api/messagesApi";
import { messageService } from "@/features/messages/services/messageService";
import { useMessageHub } from "./useMessageHub";

type ConnectionStatus =
    | "disconnected"
    | "connecting"
    | "connected"
    | "error";

export function useChannelMessages(channelId: string) {
    const [messages, setMessages] = useState<MessageResponse[]>([]);
    const [connectionStatus, setConnectionStatus] =
        useState<ConnectionStatus>("disconnected");
    const [isLoading, setIsLoading] = useState(true);
    const [isSending, setIsSending] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const loadMessages = useCallback(async () => {
        try {
            const result = await messageService.getMessages(channelId);
            setMessages(result);
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
    }, [channelId]);

    const sendMessage = useCallback(
        async (content: string) => {
            const normalizedContent = content.trim();

            if (!normalizedContent || isSending) return;

            setIsSending(true);
            setError(null);

            try {
                await messageService.sendMessage(channelId, normalizedContent);
                await loadMessages();
            } catch (requestError: unknown) {
                const message =
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to send message.";

                setError(message);
                throw requestError;
            } finally {
                setIsSending(false);
            }
        },
        [channelId, isSending, loadMessages],
    );

    const sendAttachments = useCallback(
        async (files: File[], content?: string) => {
            if (isSending || files.length === 0) return;

            setIsSending(true);
            setError(null);

            try {
                await messageService.sendAttachments(
                    channelId,
                    files,
                    content,
                );
                await loadMessages();
            } catch (requestError: unknown) {
                const message =
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to upload attachments.";

                setError(message);
                throw requestError;
            } finally {
                setIsSending(false);
            }
        },
        [channelId, isSending, loadMessages],
    );

    const deleteMessage = useCallback(
        async (messageId: string) => {
            setError(null);

            try {
                await messageService.deleteMessage(channelId, messageId);

                setMessages((currentMessages) =>
                    currentMessages.filter(
                        (message) => message.id !== messageId,
                    ),
                );
            } catch (requestError: unknown) {
                const message =
                    requestError instanceof Error
                        ? requestError.message
                        : "Unable to delete message.";

                setError(message);
                throw requestError;
            }
        },
        [channelId],
    );

    const handleMessageCreated = useCallback(() => {
        void loadMessages();
    }, [loadMessages]);

    const handleMessageDeleted = useCallback((messageId: string) => {
        setMessages((currentMessages) =>
            currentMessages.filter((message) => message.id !== messageId),
        );
    }, []);

    const handleConnected = useCallback(() => {
        void loadMessages();
    }, [loadMessages]);

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
        setError(null);
    }, [channelId]);

    useMessageHub({
        channelId,
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
        isSending,
        error,
        sendMessage,
        sendAttachments,
        deleteMessage,
        reloadMessages: loadMessages,
    };
}