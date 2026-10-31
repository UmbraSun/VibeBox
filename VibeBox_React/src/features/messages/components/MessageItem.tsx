import type { MessageResponse } from "@/api/messagesApi";
import { formatFileSize } from "@/features/messages/utils/fileValidation";
import { MessageAttachmentPreview } from "./MessageAttachmentPreview";

interface MessageItemProps {
    message: MessageResponse;
    currentUserId: string | null;
    deletingMessageId: string | null;
    onDelete: (messageId: string) => void;
    onImageClick: (url: string, fileName: string) => void;
}

export function MessageItem({
    message,
    currentUserId,
    deletingMessageId,
    onDelete,
    onImageClick,
}: MessageItemProps) {
    const isOwnMessage =
        !!currentUserId &&
        message.senderId.toLowerCase() === currentUserId.toLowerCase();

    return (
        <article className="channel-chat-message">
            <div className="channel-chat-message-header">
                <div className="channel-chat-message-author">
                    <strong>{message.senderEmail}</strong>

                    {isOwnMessage && (
                        <button
                            type="button"
                            className="channel-chat-delete-button"
                            onClick={() => onDelete(message.id)}
                            disabled={deletingMessageId !== null}
                        >
                            {deletingMessageId === message.id
                                ? "Deleting..."
                                : "Delete"}
                        </button>
                    )}
                </div>

                <time dateTime={message.createdAtUtc}>
                    {new Date(message.createdAtUtc).toLocaleString()}
                </time>
            </div>

            {message.content && <p>{message.content}</p>}

            {message.attachments?.map((attachment) => (
                <div className="channel-chat-attachment" key={attachment.id}>
                    <MessageAttachmentPreview
                        attachment={attachment}
                        onImageClick={onImageClick}
                    />
                    <span className="channel-chat-attachment-name">
                        {attachment.fileName}
                        {" · "}
                        {formatFileSize(attachment.sizeBytes)}
                    </span>
                </div>
            ))}
        </article>
    );
}