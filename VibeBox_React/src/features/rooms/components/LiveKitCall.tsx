import { useState } from "react";
import axios from "axios";
import {
    LiveKitRoom,
    PreJoin,
    VideoConference,
    type LocalUserChoices,
} from "@livekit/components-react";
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
                    <VideoConference />
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