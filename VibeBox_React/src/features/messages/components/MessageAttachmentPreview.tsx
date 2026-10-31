import { useEffect, useState } from "react";
import { messagesApi, type MessageAttachmentResponse } from "@/api/messagesApi";

interface MessageAttachmentPreviewProps {
    attachment: MessageAttachmentResponse;
    onImageClick: (url: string, fileName: string) => void;
}

export function MessageAttachmentPreview({
    attachment,
    onImageClick,
}: MessageAttachmentPreviewProps) {
    const [url, setUrl] = useState<string | null>(null);
    const [failed, setFailed] = useState(false);

    useEffect(() => {
        let active = true;
        let objectUrl: string | null = null;

        setUrl(null);
        setFailed(false);

        async function loadAttachment() {
            try {
                const blob = await messagesApi.getAttachment(attachment.id);
                objectUrl = URL.createObjectURL(blob);

                if (active) {
                    setUrl(objectUrl);
                } else {
                    URL.revokeObjectURL(objectUrl);
                }
            } catch {
                if (active) setFailed(true);
            }
        }

        void loadAttachment();

        return () => {
            active = false;
            if (objectUrl) URL.revokeObjectURL(objectUrl);
        };
    }, [attachment.id]);

    if (failed) {
        return (
            <p className="form-error">
                Unable to load attachment: {attachment.fileName}
            </p>
        );
    }

    if (!url) {
        return <p className="rooms-message">Loading {attachment.fileName}...</p>;
    }

    if (attachment.contentType.startsWith("image/")) {
        return (
            <button
                type="button"
                className="channel-chat-attachment-image-button"
                onClick={() => onImageClick(url, attachment.fileName)}
                aria-label={`View ${attachment.fileName}`}
            >
                <img
                    className="channel-chat-attachment-image"
                    src={url}
                    alt={attachment.fileName}
                    loading="lazy"
                />
            </button>
        );
    }

    if (attachment.contentType.startsWith("video/")) {
        return (
            <video
                className="channel-chat-attachment-video"
                src={url}
                controls
                preload="metadata"
            >
                Your browser does not support video playback.
            </video>
        );
    }

    return <p>Unsupported attachment format.</p>;
}