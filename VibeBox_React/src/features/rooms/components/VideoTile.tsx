
import { useEffect, useState } from "react";

interface VideoTileProps {
    stream: MediaStream | null;
    label: string;
    muted?: boolean;
}

function hasActiveVideo(stream: MediaStream | null): boolean {
    return (
        stream?.getVideoTracks().some(
            track => track.readyState === "live" && !track.muted,
        ) ?? false
    );
}

export function VideoTile({
    stream,
    label,
    muted = false,
}: VideoTileProps) {
    const [videoAvailable, setVideoAvailable] = useState(
        () => hasActiveVideo(stream),
    );

    useEffect(() => {
        const videoTracks = stream?.getVideoTracks() ?? [];

        const updateVideoState = () => {
            setVideoAvailable(hasActiveVideo(stream));
        };

        updateVideoState();

        for (const track of videoTracks) {
            track.addEventListener("mute", updateVideoState);
            track.addEventListener("unmute", updateVideoState);
            track.addEventListener("ended", updateVideoState);
        }

        return () => {
            for (const track of videoTracks) {
                track.removeEventListener("mute", updateVideoState);
                track.removeEventListener("unmute", updateVideoState);
                track.removeEventListener("ended", updateVideoState);
            }
        };
    }, [stream]);

    return (
        <div className="video-tile">
            <video
                ref={element => {
                    if (element && element.srcObject !== stream) {
                        element.srcObject = stream;
                    }
                }}
                autoPlay
                playsInline
                muted={muted}
            />

            {(!stream || !videoAvailable) && (
                <div className="video-tile-placeholder">
                    Video unavailable
                </div>
            )}

            <span className="video-tile-label">{label}</span>
        </div>
    );
}
