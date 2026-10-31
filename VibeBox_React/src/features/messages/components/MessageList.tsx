import type { MessageResponse } from "@/api/messagesApi";
import { MessageItem } from "./MessageItem";

interface MessageListProps {
    messages: MessageResponse[];
    isLoading: boolean;
    currentUserId: string | null;
    deletingMessageId: string | null;
    onDelete: (messageId: string) => void;
    onImageClick: (url: string, fileName: string) => void;
}

export function MessageList({
    messages,
    isLoading,
    currentUserId,
    deletingMessageId,
    onDelete,
    onImageClick,
}: MessageListProps) {
    if (isLoading) {
        return <p className="rooms-message">Loading messages...</p>;
    }

    if (messages.length === 0) {
        return (
            <p className="rooms-message">
                No messages yet. Start the conversation.
            </p>
        );
    }

    return (
        <>
            {messages.map((message) => (
                <MessageItem
                    key={message.id}
                    message={message}
                    currentUserId={currentUserId}
                    deletingMessageId={deletingMessageId}
                    onDelete={onDelete}
                    onImageClick={onImageClick}
                />
            ))}
        </>
    );
}