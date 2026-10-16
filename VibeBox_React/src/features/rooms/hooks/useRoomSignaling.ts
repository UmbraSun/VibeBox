
import { useEffect, useRef, useState } from "react";
import {
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
    type HubConnection,
} from "@microsoft/signalr";
import { useAuthStore } from "@/features/auth/store/authStore";

type ConnectionStatus =
    | "disconnected"
    | "connecting"
    | "connected"
    | "error";

interface SignalingEvent {
    id: number;
    message: string;
}

export function useRoomSignaling(roomId: string | undefined) {
    const [connectionStatus, setConnectionStatus] =
        useState<ConnectionStatus>("disconnected");
    const [events, setEvents] = useState<SignalingEvent[]>([]);
    const [connection, setConnection] = useState<HubConnection | null>(null);

    const eventIdRef = useRef(0);

    useEffect(() => {
        if (!roomId) {
            setConnection(null);
            setConnectionStatus("disconnected");
            return;
        }

        let active = true;
        let currentConnection: HubConnection | null = null;
        let stopPromise: Promise<void> | null = null;

        eventIdRef.current = 0;
        setConnectionStatus("connecting");
        setEvents([]);

        function addEvent(message: string) {
            if (!active) return;

            const id = ++eventIdRef.current;

            setEvents((current) => [
                ...current,
                { id, message },
            ].slice(-5));
        }

        async function stopConnection(target: HubConnection) {
            if (stopPromise) {
                await stopPromise;
                return;
            }

            if (target.state === HubConnectionState.Disconnected) {
                return;
            }

            stopPromise = target.stop();

            try {
                await stopPromise;
            } finally {
                stopPromise = null;
            }
        }

        async function joinRoom(target: HubConnection) {
            if (!active || target.state !== HubConnectionState.Connected) {
                return;
            }

            await target.invoke("JoinRoom", roomId);

            if (active && currentConnection === target) {
                setConnectionStatus("connected");
            }
        }

        async function startConnection() {
            const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL;

            if (!hubUrl) {
                throw new Error("SignalR hub URL is not configured.");
            }

            const target = new HubConnectionBuilder()
                .withUrl(hubUrl, {
                    accessTokenFactory: () =>
                        useAuthStore.getState().accessToken ?? "",
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();

            currentConnection = target;

            target.on(
                "ParticipantJoined",
                (eventRoomId: string, userId: string) => {
                    if (!active || eventRoomId !== roomId) return;

                    console.log("[SignalR] ParticipantJoined", {
                        eventRoomId,
                        userId,
                    });

                    addEvent(`Participant joined: ${userId}`);
                },
            );

            target.on(
                "ParticipantLeft",
                (eventRoomId: string, userId: string) => {
                    if (!active || eventRoomId !== roomId) return;

                    console.log("[SignalR] ParticipantLeft", {
                        eventRoomId,
                        userId,
                    });

                    addEvent(`Participant left: ${userId}`);
                },
            );

            target.onreconnecting((error) => {
                console.warn("[SignalR] Reconnecting", error);

                if (active && currentConnection === target) {
                    setConnectionStatus("connecting");
                }
            });

            target.onreconnected(async (connectionId) => {
                console.log("[SignalR] Reconnected", { connectionId });

                if (!active || currentConnection !== target) return;

                try {
                    await joinRoom(target);
                    addEvent("Connection restored.");
                } catch (error) {
                    console.error("[SignalR] Failed to rejoin room", error);

                    if (active && currentConnection === target) {
                        setConnectionStatus("error");
                        addEvent("Failed to rejoin the room.");
                    }
                }
            });

            target.onclose((error) => {
                if (error) {
                    console.error("[SignalR] Connection closed", error);
                }

                if (active && currentConnection === target) {
                    setConnectionStatus("disconnected");
                }
            });

            try {
                console.log("[SignalR] Calling connection.start()");
                await target.start();

                if (!active || currentConnection !== target) {
                    await stopConnection(target);
                    return;
                }

                setConnection(target);

                console.log("[SignalR] Connection started", {
                    connectionId: target.connectionId,
                    state: target.state,
                });

                await joinRoom(target);

                if (active && currentConnection === target) {
                    addEvent("Connected to room signaling.");
                    console.log("[SignalR] JoinRoom completed", { roomId });
                }
            } catch (error) {
                if (active && currentConnection === target) {
                    console.error("[SignalR] Connection startup failed", error);
                    setConnectionStatus("error");
                    addEvent("Unable to connect to room signaling.");
                }

                await stopConnection(target).catch((stopError: unknown) => {
                    console.error("[SignalR] Failed to stop connection", stopError);
                });
            }
        }

        void startConnection();

        return () => {
            active = false;

            const target = currentConnection;
            currentConnection = null;

            setConnection((existing) => existing === target ? null : existing);

            if (target) {
                void stopConnection(target).catch((error: unknown) => {
                    console.error("[SignalR] Cleanup failed", error);
                });
            }
        };
    }, [roomId]);

    return {
        connectionStatus,
        events,
        connection,
    };
}
