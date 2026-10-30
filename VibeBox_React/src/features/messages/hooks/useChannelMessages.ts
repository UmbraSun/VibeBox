
import { useCallback, useEffect, useState } from "react";
import axios from "axios";
import {
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
} from "@microsoft/signalr";
import {
    messagesApi,
    type MessageResponse,
} from "@/api/messagesApi";
import { useAuthStore } from "@/features/auth/store/authStore";

type ConnectionStatus =
    | "disconnected"
    | "connecting"
    | "connected"
    | "error";

function getErrorMessage(error: unknown, fallback: string): string {
    if (axios.isAxiosError(error)) {
        const data = error.response?.data;

        if (data instanceof Blob) {
            return fallback;
        }

        return data?.message ?? data?.error ?? fallback;
    }

    if (error instanceof Error) {
        return error.message;
    }

    return fallback;
}

interface MessageCreatedEvent {
    id: string;
    channelId: string;
}

export function useChannelMessages(channelId: string) {
    const [messages, setMessages] = useState<MessageResponse[]>([]);
    const [connectionStatus, setConnectionStatus] =
        useState<ConnectionStatus>("disconnected");
    const [isLoading, setIsLoading] = useState(true);
    const [isSending, setIsSending] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const loadMessages = useCallback(async () => {
        try {
            const result = await messagesApi.getChannelMessages(channelId);
            setMessages(result);
            setError(null);
        } catch (requestError: unknown) {
            setError(
                getErrorMessage(requestError, "Unable to load messages."),
            );
        } finally {
            setIsLoading(false);
        }
    }, [channelId]);

    const sendMessage = useCallback(
        async (content: string) => {
            const normalizedContent = content.trim();

            if (!normalizedContent || isSending) {
                return;
            }

            setIsSending(true);
            setError(null);

            try {
                await messagesApi.sendMessage(channelId, {
                    content: normalizedContent,
                });

                await loadMessages();
            } catch (requestError: unknown) {
                setError(
                    getErrorMessage(requestError, "Unable to send message."),
                );
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
                await messagesApi.uploadAttachments(
                    channelId,
                    files,
                    content,
                );

                await loadMessages();
            } catch (requestError: unknown) {
                setError(
                    getErrorMessage(
                        requestError,
                        "Unable to upload attachments.",
                    ),
                );

                throw requestError;
            } finally {
                setIsSending(false);
            }
        },
        [channelId, isSending, loadMessages],
    );

    useEffect(() => {
        let active = true;
        let connection:
            | ReturnType<HubConnectionBuilder["build"]>
            | null = null;

        setMessages([]);
        setIsLoading(true);
        setError(null);
        setConnectionStatus("connecting");

        async function refreshMessages() {
            try {
                const result =
                    await messagesApi.getChannelMessages(channelId);

                if (active) {
                    setMessages(result);
                    setError(null);
                }
            } catch (requestError: unknown) {
                if (active) {
                    setError(
                        getErrorMessage(
                            requestError,
                            "Unable to refresh messages.",
                        ),
                    );
                }
            } finally {
                if (active) {
                    setIsLoading(false);
                }
            }
        }

        async function startConnection() {
            const hubUrl = import.meta.env.VITE_MESSAGE_HUB_URL;

            if (!hubUrl) {
                if (active) {
                    setConnectionStatus("error");
                    setError("VITE_MESSAGE_HUB_URL is not configured.");
                    setIsLoading(false);
                }

                return;
            }

            const target = new HubConnectionBuilder()
                .withUrl(hubUrl, {
                    accessTokenFactory: () =>
                        useAuthStore.getState().accessToken ?? "",
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();

            connection = target;

            target.on(
                "MessageCreated",
                (event: MessageCreatedEvent) => {
                    if (
                        !active ||
                        event.channelId.toLowerCase() !== channelId.toLowerCase()
                    ) {
                        return;
                    }

                    void refreshMessages();
                },
            );

            async function joinChannel() {
                if (
                    !active ||
                    target.state !== HubConnectionState.Connected
                ) {
                    return;
                }

                await target.invoke("JoinChannel", channelId);

                if (active) {
                    setConnectionStatus("connected");
                }
            }

            target.onreconnecting(() => {
                if (active) {
                    setConnectionStatus("connecting");
                }
            });

            target.onreconnected(async () => {
                try {
                    await joinChannel();
                    await refreshMessages();
                } catch (joinError: unknown) {
                    console.error(
                        "[Messages] Failed to rejoin channel",
                        joinError,
                    );

                    if (active) {
                        setConnectionStatus("error");
                    }
                }
            });

            target.onclose(() => {
                if (active) {
                    setConnectionStatus("disconnected");
                }
            });

            try {
                await target.start();

                if (!active) {
                    await target.stop();
                    return;
                }

                await joinChannel();
                await refreshMessages();
            } catch (connectionError: unknown) {
                console.error(
                    "[Messages] SignalR connection failed",
                    connectionError,
                );

                if (active) {
                    setConnectionStatus("error");
                    setError(
                        getErrorMessage(
                            connectionError,
                            "Unable to connect to the message hub.",
                        ),
                    );
                    setIsLoading(false);
                }
            }
        }

        void startConnection();

        return () => {
            active = false;

            const target = connection;

            if (target) {
                void (async () => {
                    try {
                        if (
                            target.state === HubConnectionState.Connected
                        ) {
                            await target.invoke("LeaveChannel", channelId);
                        }
                    } catch {
                        // The connection may already be closed or reconnecting.
                    } finally {
                        await target.stop();
                    }
                })();
            }
        };
    }, [channelId]);

    return {
        messages,
        connectionStatus,
        isLoading,
        isSending,
        error,
        sendMessage,
        sendAttachments,
        reloadMessages: loadMessages,
    };
}
