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
    channelId: string;
}

interface UseMessageHubOptions {
    channelId: string;
    onMessageCreated: () => void;
    onMessageDeleted: (messageId: string) => void;
    onConnected: () => void;
    onStatusChange: (status: ConnectionStatus) => void;
    onError: (message: string) => void;
}

export function useMessageHub({
    channelId,
    onMessageCreated,
    onMessageDeleted,
    onConnected,
    onStatusChange,
    onError,
}: UseMessageHubOptions) {
    useEffect(() => {
        let active = true;
        let connection:
            | ReturnType<HubConnectionBuilder["build"]>
            | null = null;

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

            target.on("MessageCreated", (event: MessageHubEvent) => {
                if (
                    !active ||
                    event.channelId.toLowerCase() !== channelId.toLowerCase()
                ) {
                    return;
                }

                onMessageCreated();
            });

            target.on("MessageDeleted", (event: MessageHubEvent) => {
                if (
                    !active ||
                    event.channelId.toLowerCase() !== channelId.toLowerCase()
                ) {
                    return;
                }

                onMessageDeleted(event.id);
            });

            async function joinChannel() {
                if (
                    !active ||
                    target.state !== HubConnectionState.Connected
                ) {
                    return;
                }

                await target.invoke("JoinChannel", channelId);

                if (active) {
                    onStatusChange("connected");
                    onConnected();
                }
            }

            target.onreconnecting(() => {
                if (active) onStatusChange("connecting");
            });

            target.onreconnected(async () => {
                try {
                    await joinChannel();
                } catch (error: unknown) {
                    console.error("[Messages] Failed to rejoin channel", error);

                    if (active) {
                        onStatusChange("error");
                        onError(
                            getErrorMessage(
                                error,
                                "Unable to rejoin the message channel.",
                            ),
                        );
                    }
                }
            });

            target.onclose(() => {
                if (active) onStatusChange("disconnected");
            });

            try {
                await target.start();

                if (!active) {
                    await target.stop();
                    return;
                }

                await joinChannel();
            } catch (error: unknown) {
                console.error("[Messages] SignalR connection failed", error);

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
                        if (target.state === HubConnectionState.Connected) {
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
    }, [
        channelId,
        onMessageCreated,
        onMessageDeleted,
        onConnected,
        onStatusChange,
        onError,
    ]);
}