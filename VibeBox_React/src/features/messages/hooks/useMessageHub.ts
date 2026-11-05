import { useEffect } from "react";
import {
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
} from "@microsoft/signalr";
import { useAuthStore } from "@/features/auth/store/authStore";
import { getErrorMessage } from "@/features/messages/utils/errorUtils";

type ConnectionStatus =
    | "disconnected"
    | "connecting"
    | "connected"
    | "error";

interface MessageHubEvent {
    id: string;
    channelId?: string;
    conversationId?: string;
}

interface UseMessageHubOptions {
    channelId?: string;
    conversationId?: string;
    conversationIds?: string[];
    onMessageCreated: () => void;
    onMessageDeleted: (messageId: string) => void;
    onConnected: () => void;
    onStatusChange: (status: ConnectionStatus) => void;
    onError: (message: string) => void;
    onConversationMessageChanged?: (conversationId: string) => void;
}

export function useMessageHub({
    channelId,
    conversationId,
    conversationIds,
    onMessageCreated,
    onMessageDeleted,
    onConnected,
    onStatusChange,
    onError,
    onConversationMessageChanged,
}: UseMessageHubOptions) {
    useEffect(() => {
        const isConversationList = conversationIds !== undefined;
        const isConversation = Boolean(conversationId);

        const targetIds = isConversationList
            ? conversationIds
            : [conversationId ?? channelId].filter(
                  (id): id is string => Boolean(id),
              );

        if (targetIds.length === 0) {
            onStatusChange("disconnected");
            return;
        }

        let active = true;

        let connection:
            | ReturnType<HubConnectionBuilder["build"]>
            | null = null;

        const conversationIdSet = new Set(
            targetIds.map((id) => id.toLowerCase()),
        );

        function handleMessageCreated(event: MessageHubEvent) {
            if (!active) return;

            if (isConversationList) {
                if (
                    event.conversationId &&
                    conversationIdSet.has(event.conversationId.toLowerCase())
                ) {
                    onConversationMessageChanged?.(event.conversationId);
                }

                return;
            }

            const eventId = isConversation
                ? event.conversationId
                : event.channelId;

            if (
                eventId?.toLowerCase() === targetIds[0].toLowerCase()
            ) {
                onMessageCreated();
            }
        }

        function handleMessageDeleted(event: MessageHubEvent) {
            if (!active) return;

            if (isConversationList) {
                if (
                    event.conversationId &&
                    conversationIdSet.has(event.conversationId.toLowerCase())
                ) {
                    onConversationMessageChanged?.(event.conversationId);
                }

                return;
            }

            const eventId = isConversation
                ? event.conversationId
                : event.channelId;

            if (
                eventId?.toLowerCase() === targetIds[0].toLowerCase()
            ) {
                onMessageDeleted(event.id);
            }
        }

        onStatusChange("connecting");

        async function startConnection() {
            const hubUrl = import.meta.env.VITE_MESSAGE_HUB_URL;

            if (!hubUrl) {
                if (active) {
                    onStatusChange("error");
                    onError("VITE_MESSAGE_HUB_URL is not configured.");
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

            target.on("MessageCreated", handleMessageCreated);
            target.on("MessageDeleted", handleMessageDeleted);

            async function joinTargets() {
                if (
                    !active ||
                    target.state !== HubConnectionState.Connected
                ) {
                    return;
                }

                for (const id of targetIds) {
                    await target.invoke(
                        isConversationList || isConversation
                            ? "JoinConversation"
                            : "JoinChannel",
                        id,
                    );
                }

                if (active) {
                    onStatusChange("connected");
                    onConnected();
                }
            }

            target.onreconnecting(() => {
                if (active) {
                    onStatusChange("connecting");
                }
            });

            target.onreconnected(async () => {
                try {
                    await joinTargets();
                } catch (error: unknown) {
                    console.error(
                        "[Messages] Failed to rejoin message groups",
                        error,
                    );

                    if (active) {
                        onStatusChange("error");
                        onError(
                            getErrorMessage(
                                error,
                                "Unable to rejoin message groups.",
                            ),
                        );
                    }
                }
            });

            target.onclose(() => {
                if (active) {
                    onStatusChange("disconnected");
                }
            });

            try {
                await target.start();

                if (!active) {
                    await target.stop();
                    return;
                }

                await joinTargets();
            } catch (error: unknown) {
                console.error(
                    "[Messages] SignalR connection failed",
                    error,
                );

                if (active) {
                    onStatusChange("error");
                    onError(
                        getErrorMessage(
                            error,
                            "Unable to connect to the message hub.",
                        ),
                    );
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
                            for (const id of targetIds) {
                                await target.invoke(
                                    isConversationList || isConversation
                                        ? "LeaveConversation"
                                        : "LeaveChannel",
                                    id,
                                );
                            }
                        }
                    } catch {
                        // The connection may already be closed or reconnecting.
                    } finally {
                        await target.stop();
                    }
                })();
            }
        };
    }, [
        channelId,
        conversationId,
        conversationIds,
        onMessageCreated,
        onMessageDeleted,
        onConnected,
        onStatusChange,
        onError,
        onConversationMessageChanged,
    ]);
}