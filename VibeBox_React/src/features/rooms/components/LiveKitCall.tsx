
import { useEffect, useState } from "react";
import axios from "axios";
import {
    LiveKitRoom,
    VideoConference,
} from "@livekit/components-react";
import { roomsApi } from "@/api/roomsApi";

import "@livekit/components-styles";

interface LiveKitCallProps {
    roomId: string;
}

export function LiveKitCall({ roomId }: LiveKitCallProps) {
    const [serverUrl, setServerUrl] = useState("");
    const [token, setToken] = useState("");
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [retry, setRetry] = useState(0);

    useEffect(() => {
        let cancelled = false;

        async function loadToken() {
            setIsLoading(true);
            setError(null);
            setToken("");
            setServerUrl("");

            try {
                const result = await roomsApi.getLiveKitToken(roomId);

                if (cancelled) return;

                if (!result.serverUrl || !result.participantToken) {
                    throw new Error(
                        "The server returned an incomplete LiveKit configuration.",
                    );
                }

                setServerUrl(result.serverUrl);
                setToken(result.participantToken);
            } catch (requestError: unknown) {
                if (cancelled) return;

                const message = axios.isAxiosError(requestError)
                    ? requestError.response?.data?.message
                    : requestError instanceof Error
                        ? requestError.message
                        : null;

                setError(
                    typeof message === "string"
                        ? message
                        : "Unable to connect to the video room.",
                );
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        void loadToken();

        return () => {
            cancelled = true;
        };
    }, [roomId, retry]);

    if (isLoading) {
        return (
            <p className="rooms-message">
                Connecting to video room...
            </p>
        );
    }

    if (error) {
        return (
            <div>
                <p className="form-error" role="alert">
                    {error}
                </p>

                <button
                    type="button"
                    className="secondary-button"
                    onClick={() => setRetry((value) => value + 1)}
                >
                    Try again
                </button>
            </div>
        );
    }

    if (!serverUrl || !token) {
        return (
            <p className="form-error" role="alert">
                LiveKit connection details are missing.
            </p>
        );
    }

    return (
        <div className="livekit-call">
            <LiveKitRoom
                serverUrl={serverUrl}
                token={token}
                connect
                audio
                video
                onError={(connectionError) => {
                    console.error(
                        "LiveKit connection error:",
                        connectionError,
                    );

                    setError("Unable to establish the video connection.");
                }}
            >
                <VideoConference />
            </LiveKitRoom>
        </div>
    );
}
