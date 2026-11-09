import type { MessageResponse } from "@/api/messagesApi";
import { MessageItem } from "./MessageItem";

interface MessageListProps {
    messages: MessageResponse[];
    isLoading: boolean;
    isLoadingOlder: boolean;
    unreadMessageId: string | null;
    currentUserId: string | null;
    deletingMessageId: string | null;
    onDelete: (messageId: string) => void;
    onImageClick: (url: string, fileName: string) => void;
}

export function MessageList({
    messages,
    isLoading,
    isLoadingOlder,
    unreadMessageId,
    currentUserId,
    deletingMessageId,
    onDelete,
    onImageClick,
}: MessageListProps) {
    if (isLoading) {
        return <p>Loading messages...</p>;
    }

    if (messages.length === 0) {
        return <p>No messages yet. Start the conversation!</p>;
    }

    return (
        <>
            {isLoadingOlder && (
                <p className="channel-chat-loading-older">
                    Loading older messages...
                </p>
            )}

            {messages.map((message) => (
                <div key={message.id}>
                    {message.id === unreadMessageId && (
                        <div className="channel-chat-unread-divider">
                            <span>Unread messages</span>
                        </div>
                    )}

                    <MessageItem
                        message={message}
                        currentUserId={currentUserId}
                        deletingMessageId={deletingMessageId}
                        onDelete={onDelete}
                        onImageClick={onImageClick}
                    />
                </div>
            ))}
        </>
    );
}