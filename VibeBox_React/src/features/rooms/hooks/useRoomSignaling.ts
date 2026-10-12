import { useEffect, useState } from "react";
import {
    HubConnectionBuilder,
    HubConnectionState,
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

    useEffect(() => {
        if (!roomId) return;

        let active = true;
        let eventId = 0;
        let connection: HubConnection | null = null;

        setConnectionStatus("connecting");
        setEvents([]);

        function addEvent(message: string) {
            if (!active) return;

            setEvents((current) => [
                ...current,
                { id: ++eventId, message },
            ].slice(-5));
        }

        async function joinRoom(currentConnection: HubConnection) {
            await currentConnection.invoke("JoinRoom", roomId);

            if (active) {
                setConnectionStatus("connected");
            }
        }

        async function startConnection() {
            const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL;

            if (!hubUrl) {
                throw new Error("SignalR hub URL is not configured.");
            }

            connection = new HubConnectionBuilder()
                .withUrl(hubUrl, {
                    accessTokenFactory: () =>
                        useAuthStore.getState().accessToken ?? "",
                })
                .withAutomaticReconnect()
                .build();

            connection.on(
                "ParticipantJoined",
                (eventRoomId: string, userId: string) => {
                    if (eventRoomId !== roomId) return;

                    addEvent(`Participant joined: ${userId}`);
                },
            );

            connection.on(
                "ParticipantLeft",
                (eventRoomId: string, userId: string) => {
                    if (eventRoomId !== roomId) return;

                    addEvent(`Participant left: ${userId}`);
                },
            );

            connection.onreconnecting(() => {
                if (active) {
                    setConnectionStatus("connecting");
                }
            });

            connection.onreconnected(async () => {
                if (!active || !connection) return;

                try {
                    await joinRoom(connection);
                    addEvent("Connection restored.");
                } catch {
                    if (active) {
                        setConnectionStatus("error");
                        addEvent("Failed to rejoin the room.");
                    }
                }
            });

            connection.onclose(() => {
                if (active) {
                    setConnectionStatus("disconnected");
                }
            });

            await connection.start();

            if (!active) {
                await connection.stop();
                return;
            }

            await joinRoom(connection);
            addEvent("Connected to room signaling.");
        }

        void startConnection().catch(() => {
            if (active) {
                setConnectionStatus("error");
                addEvent("Unable to connect to room signaling.");
            }
        });

        return () => {
            active = false;

            if (connection?.state !== undefined &&
                connection.state !== HubConnectionState.Disconnected) {
                void connection.stop();
            }
        };
    }, [roomId]);

    return {
        connectionStatus,
        events,
    };
}