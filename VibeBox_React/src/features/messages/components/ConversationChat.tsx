import { useState } from "react";
import { useAuthStore } from "@/features/auth/store/authStore";
import { useConversationMessages } from "@/features/messages/hooks/useConversationMessages";
import { getCurrentUserId } from "@/features/messages/utils/tokenUtils";
import { ImageLightbox } from "./ImageLightbox";
import { MessageComposer } from "./MessageComposer";
import { MessageList } from "./MessageList";

interface ConversationChatProps {
    conversationId: string;
    conversationName: string;
}

export function ConversationChat({
    conversationId,
    conversationName,
}: ConversationChatProps) {
    const accessToken = useAuthStore((state) => state.accessToken);
    const currentUserId = getCurrentUserId(accessToken);

    const [deletingMessageId, setDeletingMessageId] =
        useState<string | null>(null);

    const [lightbox, setLightbox] = useState<{
        url: string;
        fileName: string;
    } | null>(null);

    const {
        messages,
        connectionStatus,
        isLoading,
        isSending,
        error,
        sendMessage,
        sendAttachments,
        deleteMessage,
    } = useConversationMessages(conversationId);

    async function handleSend(content: string, files: File[]) {
        if (files.length > 0) {
            await sendAttachments(files, content);
        } else {
            await sendMessage(content);
        }
    }

    async function handleDeleteMessage(messageId: string) {
        const confirmed = window.confirm(
            "Are you sure you want to delete this message and its attachments?",
        );

        if (!confirmed) return;

        setDeletingMessageId(messageId);

        try {
            await deleteMessage(messageId);
        } catch {
            // Error is already handled by the hook.
        } finally {
            setDeletingMessageId(null);
        }
    }

    return (
        <section className="rooms-section channel-chat">
            <div className="rooms-heading">
                <div>
                    <p className="room-eyebrow">CONVERSATION</p>
                    <h2>{conversationName}</h2>
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
                <MessageList
                    messages={messages}
                    isLoading={isLoading}
                    currentUserId={currentUserId}
                    deletingMessageId={deletingMessageId}
                    onDelete={handleDeleteMessage}
                    onImageClick={(url, fileName) =>
                        setLightbox({ url, fileName })
                    }
                />
            </div>

            <MessageComposer
                isSending={isSending}
                onSend={handleSend}
            />

            {lightbox && (
                <ImageLightbox
                    url={lightbox.url}
                    fileName={lightbox.fileName}
                    onClose={() => setLightbox(null)}
                />
            )}
        </section>
    );
}