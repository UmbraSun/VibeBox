
import type { MessageResponse } from "@/api/messagesApi";
import { formatFileSize } from "@/features/messages/utils/fileValidation";
import { MessageAttachmentPreview } from "./MessageAttachmentPreview";

interface MessageItemProps {
    message: MessageResponse;
    currentUserId: string | null;
    deletingMessageId: string | null;
    onDelete: (messageId: string) => void;
    onReply: (message: MessageResponse) => void;
    onJumpToMessage: (messageId: string) => void;
    onImageClick: (url: string, fileName: string) => void;
}

export function MessageItem({
    message,
    currentUserId,
    deletingMessageId,
    onDelete,
    onReply,
    onJumpToMessage,
    onImageClick,
}: MessageItemProps) {
    const isOwnMessage =
        !!currentUserId &&
        message.senderId.toLowerCase() === currentUserId.toLowerCase();

    return (
        <article
            className="channel-chat-message"
            data-message-id={message.id}
            data-message-created-at={message.createdAtUtc}
        >
            {message.replyToMessageId && (
                <button
                    type="button"
                    className="channel-chat-reply-preview"
                    onClick={() =>
                        onJumpToMessage(message.replyToMessageId!)
                    }
                >
                    <strong>
                        {message.replyToMessage?.senderEmail ??
                            "Original message"}
                    </strong>

                    <span>
                        {message.replyToMessage?.content ??
                            "Original message is unavailable"}
                    </span>
                </button>
            )}

            <div className="channel-chat-message-header">
                <div className="channel-chat-message-author">
                    <strong>{message.senderEmail}</strong>

                    <button
                        type="button"
                        onClick={() => onReply(message)}
                        disabled={deletingMessageId !== null}
                    >
                        Reply
                    </button>

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
