
import { useEffect, useRef, useState } from "react";
import { messagesApi, type MessageAttachmentResponse } from "@/api/messagesApi";

interface MessageAttachmentPreviewProps {
    attachment: MessageAttachmentResponse;
    onImageClick: (url: string, fileName: string) => void;
}

export function MessageAttachmentPreview({
    attachment,
    onImageClick,
}: MessageAttachmentPreviewProps) {
    const containerRef = useRef<HTMLDivElement>(null);

    const [shouldLoad, setShouldLoad] = useState(false);
    const [url, setUrl] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [failed, setFailed] = useState(false);

    const isImage = attachment.contentType.startsWith("image/");
    const isVideo = attachment.contentType.startsWith("video/");

    // Load images only when they approach the visible area.
    useEffect(() => {
        if (!isImage) return;

        const element = containerRef.current;

        if (!element || !("IntersectionObserver" in window)) {
            setShouldLoad(true);
            return;
        }

        const observer = new IntersectionObserver(
            (entries) => {
                if (entries.some((entry) => entry.isIntersecting)) {
                    setShouldLoad(true);
                    observer.disconnect();
                }
            },
            { rootMargin: "300px" },
        );

        observer.observe(element);

        return () => observer.disconnect();
    }, [attachment.id, isImage]);

    // Download only when the image approaches the viewport
    // or the user explicitly requests a video.
    useEffect(() => {
        if (!shouldLoad) return;

        const controller = new AbortController();
        let active = true;
        let objectUrl: string | null = null;

        setFailed(false);
        setUrl(null);
        setIsLoading(true);

        async function loadAttachment() {
            try {
                const blob = await messagesApi.getAttachment(
                    attachment.id,
                    controller.signal,
                );

                if (!active) return;

                objectUrl = URL.createObjectURL(blob);
                setUrl(objectUrl);
            } catch {
                if (active && !controller.signal.aborted) {
                    setFailed(true);
                }
            } finally {
                if (active) {
                    setIsLoading(false);
                }
            }
        }

        void loadAttachment();

        return () => {
            active = false;
            controller.abort();

            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
            }
        };
    }, [attachment.id, shouldLoad]);

    if (!isImage && !isVideo) {
        return <p>Unsupported attachment format.</p>;
    }

    if (failed) {
        return (
            <p className="form-error" role="alert">
                Unable to load attachment: {attachment.fileName}
            </p>
        );
    }

    if (!shouldLoad && isVideo) {
        return (
            <div ref={containerRef}>
                <button
                    type="button"
                    onClick={() => setShouldLoad(true)}
                >
                    Load video: {attachment.fileName}
                </button>
            </div>
        );
    }

    if (!shouldLoad || isLoading || !url) {
        return (
            <div ref={containerRef}>
                <p className="rooms-message">
                    Loading {attachment.fileName}...
                </p>
            </div>
        );
    }

    if (isImage) {
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
                />
            </button>
        );
    }

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
