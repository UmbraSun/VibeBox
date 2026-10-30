
import { useEffect, useState, type SyntheticEvent } from "react";
import {
    messagesApi,
    type MessageAttachmentResponse,
} from "@/api/messagesApi";
import {
    useChannelMessages,
} from "@/features/messages/hooks/useChannelMessages";

interface ChannelChatProps {
    channelId: string;
    channelName: string;
}

const MAX_FILES_COUNT = 5;
const MAX_IMAGE_SIZE = 10 * 1024 * 1024;
const MAX_VIDEO_SIZE = 100 * 1024 * 1024;
const MAX_TOTAL_SIZE = 150 * 1024 * 1024;

function formatFileSize(sizeBytes: number): string {
    if (sizeBytes < 1024 * 1024) {
        return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`;
    }

    return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`;
}

function validateFiles(files: File[]): string | null {
    if (files.length > MAX_FILES_COUNT) {
        return `You can select a maximum of ${MAX_FILES_COUNT} files.`;
    }

    let totalSize = 0;

    for (const file of files) {
        const isVideo = file.type.startsWith("video/");
        const isImage = file.type.startsWith("image/");

        if (!isVideo && !isImage) {
            return `Unsupported file type: ${file.name}.`;
        }

        const maxSize = isVideo ? MAX_VIDEO_SIZE : MAX_IMAGE_SIZE;

        if (file.size === 0 || file.size > maxSize) {
            return isVideo
                ? `Video "${file.name}" must not exceed 100 MB.`
                : `Image "${file.name}" must not exceed 10 MB.`;
        }

        totalSize += file.size;

        if (totalSize > MAX_TOTAL_SIZE) {
            return "Total attachment size must not exceed 150 MB.";
        }
    }

    return null;
}

function MessageAttachmentPreview({
    attachment,
    onImageClick,
}: {
    attachment: MessageAttachmentResponse;
    onImageClick: (url: string, fileName: string) => void;
}) {
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
                if (active) {
                    setFailed(true);
                }
            }
        }

        void loadAttachment();

        return () => {
            active = false;

            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
            }
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
        return (
            <p className="rooms-message">
                Loading {attachment.fileName}...
            </p>
        );
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

export function ChannelChat({
    channelId,
    channelName,
}: ChannelChatProps) {
    const [content, setContent] = useState("");
    const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
    const [fileError, setFileError] = useState<string | null>(null);
    const [lightbox, setLightbox] = useState<{
        url: string;
        fileName: string;
    } | null>(null);

    useEffect(() => {
        if (!lightbox) return;

        function handleKeyDown(event: KeyboardEvent) {
            if (event.key === "Escape") {
                setLightbox(null);
            }
        }

        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";

        window.addEventListener("keydown", handleKeyDown);

        return () => {
            window.removeEventListener("keydown", handleKeyDown);
            document.body.style.overflow = previousOverflow;
        };
    }, [lightbox]);

    const {
        messages,
        connectionStatus,
        isLoading,
        isSending,
        error,
        sendMessage,
        sendAttachments,
    } = useChannelMessages(channelId);

    async function handleSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const message = content.trim();

        if (isSending || (!message && selectedFiles.length === 0)) {
            return;
        }

        try {
            if (selectedFiles.length > 0) {
                await sendAttachments(selectedFiles, message);
            } else {
                await sendMessage(message);
            }

            setContent("");
            setSelectedFiles([]);
            setFileError(null);
        } catch {
            // Error is already handled by the hook.
        }
    }

    function handleFilesChange(
        event: SyntheticEvent<HTMLInputElement>,
    ) {
        const files = Array.from(event.currentTarget.files ?? []);
        event.currentTarget.value = "";

        if (files.length === 0) {
            return;
        }

        setSelectedFiles((currentFiles) => {
            const mergedFiles = [...currentFiles];

            for (const file of files) {
                const alreadySelected = mergedFiles.some(
                    (existingFile) =>
                        existingFile.name === file.name &&
                        existingFile.size === file.size &&
                        existingFile.lastModified === file.lastModified,
                );

                if (!alreadySelected) {
                    mergedFiles.push(file);
                }
            }

            const validationError = validateFiles(mergedFiles);

            if (validationError) {
                setFileError(validationError);
                return currentFiles;
            }

            setFileError(null);
            return mergedFiles;
        });
    }

    function removeSelectedFile(indexToRemove: number) {
        setSelectedFiles((currentFiles) => {
            const updatedFiles = currentFiles.filter(
                (_, index) => index !== indexToRemove,
            );

            setFileError(validateFiles(updatedFiles));

            return updatedFiles;
        });
    }

    return (
        <section className="rooms-section channel-chat">
            <div className="rooms-heading">
                <div>
                    <p className="room-eyebrow">TEXT CHANNEL</p>
                    <h2>{channelName}</h2>
                </div>

                <span className="participant-count">
                    {connectionStatus === "connected"
                        ? "Connected"
                        : connectionStatus === "connecting"
                            ? "Connecting..."
                            : connectionStatus === "error"
                                ? "Connection error"
                                : "Disconnected"}
                </span>
            </div>

            {(error || fileError) && (
                <p className="form-error" role="alert">
                    {fileError ?? error}
                </p>
            )}

            <div className="channel-chat-messages" aria-live="polite">
                {isLoading ? (
                    <p className="rooms-message">Loading messages...</p>
                ) : messages.length === 0 ? (
                    <p className="rooms-message">
                        No messages yet. Start the conversation.
                    </p>
                ) : (
                    messages.map((message) => (
                        <article
                            className="channel-chat-message"
                            key={message.id}
                        >
                            <div className="channel-chat-message-header">
                                <strong>{message.senderEmail}</strong>
                                <time dateTime={message.createdAtUtc}>
                                    {new Date(
                                        message.createdAtUtc,
                                    ).toLocaleString()}
                                </time>
                            </div>

                            {message.content && <p>{message.content}</p>}

                            {message.attachments?.map((attachment) => (
                                <div
                                    className="channel-chat-attachment"
                                    key={attachment.id}
                                >
                                    <MessageAttachmentPreview
                                        attachment={attachment}
                                        onImageClick={(url, fileName) =>
                                            setLightbox({ url, fileName })
                                        }
                                    />
                                    <span className="channel-chat-attachment-name">
                                        {attachment.fileName}
                                        {" · "}
                                        {formatFileSize(attachment.sizeBytes)}
                                    </span>
                                </div>
                            ))}
                        </article>
                    ))
                )}
            </div>

            <form
                className="room-form channel-chat-form"
                onSubmit={handleSubmit}
            >
                <label className="room-name-field">
                    Message
                    <textarea
                        value={content}
                        onChange={(event) => setContent(event.target.value)}
                        placeholder="Write a message..."
                        maxLength={4000}
                        rows={3}
                        disabled={isSending}
                    />
                </label>

                <label className="room-name-field">
                    Attach images, GIFs or videos (max. 5 files)
                    <input
                        type="file"
                        accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm"
                        multiple
                        onChange={handleFilesChange}
                        disabled={isSending}
                    />
                </label>

                {selectedFiles.length > 0 && (
                    <ul className="channel-chat-selected-files">
                        {selectedFiles.map((file, index) => (
                            <li key={`${file.name}-${file.size}-${index}`}>
                                <span>
                                    {file.name}
                                    {" · "}
                                    {formatFileSize(file.size)}
                                </span>

                                <button
                                    type="button"
                                    onClick={() => removeSelectedFile(index)}
                                    disabled={isSending}
                                >
                                    Remove
                                </button>
                            </li>
                        ))}
                    </ul>
                )}

                <button
                    type="submit"
                    disabled={
                        isSending ||
                        (!content.trim() && selectedFiles.length === 0)
                    }
                >
                    {isSending
                        ? "Sending..."
                        : selectedFiles.length > 0
                            ? `Send (${selectedFiles.length} file${selectedFiles.length === 1 ? "" : "s"})`
                            : "Send"}
                </button>
            </form>

            {lightbox && (
                <div
                    className="channel-chat-lightbox"
                    onClick={() => setLightbox(null)}
                    role="presentation"
                >
                    <button
                        type="button"
                        className="channel-chat-lightbox-close"
                        onClick={() => setLightbox(null)}
                        aria-label="Close image preview"
                    >
                        &times;
                    </button>

                    <div
                        className="channel-chat-lightbox-content"
                        role="dialog"
                        aria-modal="true"
                        aria-label={lightbox.fileName}
                        onClick={(event) => event.stopPropagation()}
                    >
                        <img
                            src={lightbox.url}
                            alt={lightbox.fileName}
                            className="channel-chat-lightbox-image"
                        />

                        <span>{lightbox.fileName}</span>
                    </div>
                </div>
            )}
        </section>
    );
}
