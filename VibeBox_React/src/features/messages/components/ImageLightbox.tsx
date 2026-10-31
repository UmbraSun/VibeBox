
import { useEffect } from "react";

interface ImageLightboxProps {
    url: string;
    fileName: string;
    onClose: () => void;
}

export function ImageLightbox({
    url,
    fileName,
    onClose,
}: ImageLightboxProps) {
    useEffect(() => {
        function handleKeyDown(event: KeyboardEvent) {
            if (event.key === "Escape") onClose();
        }

        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";
        window.addEventListener("keydown", handleKeyDown);

        return () => {
            document.body.style.overflow = previousOverflow;
            window.removeEventListener("keydown", handleKeyDown);
        };
    }, [onClose]);

    return (
        <div
            className="channel-chat-lightbox"
            onClick={onClose}
            role="presentation"
        >
            <button
                type="button"
                className="channel-chat-lightbox-close"
                onClick={onClose}
                aria-label="Close image preview"
            >
                &times;
            </button>

            <div
                className="channel-chat-lightbox-content"
                role="dialog"
                aria-modal="true"
                aria-label={fileName}
                onClick={(event) => event.stopPropagation()}
            >
                <img
                    src={url}
                    alt={fileName}
                    className="channel-chat-lightbox-image"
                />
                <span>{fileName}</span>
            </div>
        </div>
    );
}