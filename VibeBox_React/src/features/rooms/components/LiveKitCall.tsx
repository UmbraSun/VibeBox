
import { useState } from "react";
import axios from "axios";
import {
    LiveKitRoom,
    PreJoin,
    VideoConference,
    useParticipants,
    type LocalUserChoices,
} from "@livekit/components-react";
import type { Participant } from "livekit-client";
import { channelsApi } from "@/api/channelsApi";
import { useAuthStore } from "@/features/auth/store/authStore";
import "@livekit/components-styles";

interface LiveKitCallProps {
    channelId: string;
}

interface ConnectionDetails {
    serverUrl: string;
    participantToken: string;
}

function LiveKitParticipants() {
    const participants = useParticipants();

    return (
        <aside className="livekit-participants">
            <div className="livekit-participants__header">
                <h3>Participants</h3>
                <span>{participants.length}</span>
            </div>

            <div className="livekit-participants__list">
                {participants.map((participant: Participant) => {
                    const name = participant.name || participant.identity;
                    const initials = name.slice(0, 2).toUpperCase();

                    return (
                        <div
                            key={participant.identity}
                            className={`livekit-participants__item ${participant.isSpeaking
                                    ? "is-speaking"
                                    : ""
                                }`}
                        >
                            <div className="livekit-participants__avatar">
                                {initials}
                            </div>

                            <div className="livekit-participants__info">
                                <span className="livekit-participants__name">
                                    {name}
                                </span>

                                {participant.isLocal && (
                                    <span className="livekit-participants__you">
                                        You
                                    </span>
                                )}

                                {participant.isSpeaking && (
                                    <span className="livekit-participants__speaking">
                                        Speaking
                                    </span>
                                )}
                            </div>

                            <div className="livekit-participants__devices">
                                <span
                                    className={`livekit-participants__device ${participant.isMicrophoneEnabled
                                            ? "is-enabled"
                                            : "is-disabled"
                                        }`}
                                    title={
                                        participant.isMicrophoneEnabled
                                            ? "Microphone on"
                                            : "Microphone off"
                                    }
                                >
                                    {participant.isMicrophoneEnabled ? "🎤" : "🔇"}
                                </span>

                                <span
                                    className={`livekit-participants__device ${participant.isCameraEnabled
                                            ? "is-enabled"
                                            : "is-disabled"
                                        }`}
                                    title={
                                        participant.isCameraEnabled
                                            ? "Camera on"
                                            : "Camera off"
                                    }
                                >
                                    {participant.isCameraEnabled ? "📷" : "🚫"}
                                </span>
                            </div>
                        </div>
                    );
                })}

                {participants.length === 0 && (
                    <p className="rooms-message">
                        No participants yet.
                    </p>
                )}
            </div>
        </aside>
    );
}

export function LiveKitCall({ channelId }: LiveKitCallProps) {
    const accessToken = useAuthStore((state) => state.accessToken);

    const defaultUsername = (() => {
        if (!accessToken) return "Guest";

        try {
            const payload = accessToken.split(".")[1];
            const claims = JSON.parse(
                atob(payload.replace(/-/g, "+").replace(/_/g, "/")),
            ) as { email?: string };

            return claims.email?.split("@")[0] || "Guest";
        } catch {
            return "Guest";
        }
    })();

    const [connection, setConnection] =
        useState<ConnectionDetails | null>(null);
    const [choices, setChoices] = useState<LocalUserChoices | null>(null);
    const [isJoining, setIsJoining] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function handleJoin(selectedChoices: LocalUserChoices) {
        setIsJoining(true);
        setError(null);

        try {
            const result = await channelsApi.getLiveKitToken(channelId);

            if (!result.serverUrl || !result.participantToken) {
                throw new Error(
                    "The server returned incomplete LiveKit connection details.",
                );
            }

            setChoices(selectedChoices);
            setConnection({
                serverUrl: result.serverUrl,
                participantToken: result.participantToken,
            });
        } catch (requestError: unknown) {
            const message = axios.isAxiosError(requestError)
                ? requestError.response?.data?.message
                : requestError instanceof Error
                    ? requestError.message
                    : null;

            setError(
                typeof message === "string"
                    ? message
                    : "Unable to join the video room. Please try again.",
            );
        } finally {
            setIsJoining(false);
        }
    }

    if (connection && choices) {
        return (
            <div className="livekit-call">
                <LiveKitRoom
                    serverUrl={connection.serverUrl}
                    token={connection.participantToken}
                    connect
                    audio={choices.audioEnabled}
                    video={choices.videoEnabled}
                    onDisconnected={() => {
                        setConnection(null);
                        setChoices(null);
                        setError(null);
                    }}
                    onError={(connectionError) => {
                        console.error(
                            "LiveKit connection error:",
                            connectionError,
                        );
                        setError("Unable to establish the video connection.");
                        setConnection(null);
                        setChoices(null);
                    }}
                >
                    <div className="livekit-call__content">
                        <div className="livekit-call__conference">
                            <VideoConference />
                        </div>

                        <LiveKitParticipants />
                    </div>
                </LiveKitRoom>
            </div>
        );
    }

    return (
        <div className="livekit-prejoin">
            <h3>Before joining</h3>
            <p className="rooms-message">
                Check your camera and microphone settings before entering the room.
            </p>

            {error && (
                <p className="form-error" role="alert">
                    {error}
                </p>
            )}

            {isJoining ? (
                <p className="rooms-message">Joining video room...</p>
            ) : (
                <PreJoin
                    defaults={{
                        username: defaultUsername,
                        audioEnabled: false,
                        videoEnabled: false,
                    }}
                    onSubmit={(selectedChoices) => {
                        void handleJoin(selectedChoices);
                    }}
                />
            )}
        </div>
    );
}
