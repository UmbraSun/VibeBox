
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

function formatFileSize(sizeBytes: number): string {
    if (sizeBytes < 1024 * 1024) {
        return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`;
    }

    return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`;
}

function MessageAttachmentPreview({
    attachment,
}: {
    attachment: MessageAttachmentResponse;
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
            <a
                className="channel-chat-attachment-link"
                href={url}
                target="_blank"
                rel="noreferrer"
            >
                <img
                    className="channel-chat-attachment-image"
                    src={url}
                    alt={attachment.fileName}
                    loading="lazy"
                />
            </a>
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

    const {
        messages,
        connectionStatus,
        isLoading,
        isSending,
        error,
        sendMessage,
        sendAttachment,
    } = useChannelMessages(channelId);

    async function handleSubmit(event: SyntheticEvent<HTMLFormElement>) {
        event.preventDefault();

        const message = content.trim();

        if ((!message && selectedFiles.length === 0) || isSending) {
            return;
        }

        try {
            if (message) {
                await sendMessage(message);
                setContent("");
            }

            for (const file of selectedFiles) {
                await sendAttachment(file);
            }

            setSelectedFiles([]);
        } catch {
            // The hook exposes the request error to the UI.
        }
    }

    function handleFilesChange(
        event: SyntheticEvent<HTMLInputElement>,
    ) {
        const files = Array.from(event.currentTarget.files ?? []);
        setSelectedFiles(files);

        // Allows selecting the same file again after removing or uploading it.
        event.currentTarget.value = "";
    }

    function removeSelectedFile(indexToRemove: number) {
        setSelectedFiles((currentFiles) =>
            currentFiles.filter((_, index) => index !== indexToRemove),
        );
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

            {error && (
                <p className="form-error" role="alert">
                    {error}
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
                    />
                </label>

                <label className="room-name-field">
                    Attach images, GIFs or videos
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
        </section>
    );
}
