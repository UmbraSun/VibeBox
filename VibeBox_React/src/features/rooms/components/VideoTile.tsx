interface VideoTileProps {
    stream: MediaStream | null;
    label: string;
    muted?: boolean;
}

export function VideoTile({
    stream,
    label,
    muted = false,
}: VideoTileProps) {
    return (
        <div className="video-tile">
            <video
                ref={(element) => {
                    if (element && element.srcObject !== stream) {
                        element.srcObject = stream;
                    }
                }}
                autoPlay
                playsInline
                muted={muted}
            />

            {!stream && (
                <div className="video-tile-placeholder">
                    Video unavailable
                </div>
            )}

            <span className="video-tile-label">{label}</span>
        </div>
    );
}
