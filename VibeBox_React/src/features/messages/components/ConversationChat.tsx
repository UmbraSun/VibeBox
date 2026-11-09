
import {
    useCallback,
    useEffect,
    useLayoutEffect,
    useRef,
    useState,
} from "react";
import { conversationsApi } from "@/api/conversationsApi";
import { useAuthStore } from "@/features/auth/store/authStore";
import { useConversationMessages } from "@/features/messages/hooks/useConversationMessages";
import { getCurrentUserId } from "@/features/messages/utils/tokenUtils";
import { GroupConversationPanel } from "@/features/conversations/components/GroupConversationPanel";
import { ImageLightbox } from "./ImageLightbox";
import { MessageComposer } from "./MessageComposer";
import { MessageList } from "./MessageList";

interface ConversationChatProps {
    conversationId: string;
    conversationName: string;
    conversationType: string;
    lastReadAtUtc: string | null;
    onLeaveGroup: () => void;
    onConversationRead?: (conversationId: string) => void;
}

export function ConversationChat({
    conversationId,
    conversationName,
    conversationType,
    lastReadAtUtc,
    onLeaveGroup,
    onConversationRead,
}: ConversationChatProps) {
    const accessToken = useAuthStore((state) => state.accessToken);
    const currentUserId = getCurrentUserId(accessToken);

    const [deletingMessageId, setDeletingMessageId] =
        useState<string | null>(null);

    const [lightbox, setLightbox] = useState<{
        url: string;
        fileName: string;
    } | null>(null);

    const [isGroupPanelOpen, setIsGroupPanelOpen] = useState(false);

    const messagesContainerRef = useRef<HTMLDivElement>(null);
    const initialScrollDoneRef = useRef(false);

    const pendingScrollRef = useRef<{
        scrollHeight: number;
        scrollTop: number;
    } | null>(null);

    const hasReachedUnreadStartRef = useRef(false);
    const lastSubmittedReadAtRef = useRef<string | null>(null);
    const readTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const isGroup = conversationType.toLowerCase() === "group";

    const {
        messages,
        connectionStatus,
        isLoading,
        isLoadingOlder,
        hasMoreMessages,
        isSending,
        error,
        sendMessage,
        sendAttachments,
        deleteMessage,
        loadOlderMessages,
    } = useConversationMessages(conversationId);

    const unreadMessageId =
        messages.find((message) => {
            const isIncoming =
                message.senderId.toLowerCase() !== currentUserId?.toLowerCase();

            if (!isIncoming) return false;

            if (!lastReadAtUtc) return true;

            return (
                new Date(message.createdAtUtc).getTime() >
                new Date(lastReadAtUtc).getTime()
            );
        })?.id ?? null;

    const handleMessagesScroll = useCallback(() => {
        const container = messagesContainerRef.current;

        if (
            !container ||
            container.scrollTop > 80 ||
            isLoadingOlder ||
            !hasMoreMessages
        ) {
            return;
        }

        pendingScrollRef.current = {
            scrollHeight: container.scrollHeight,
            scrollTop: container.scrollTop,
        };

        void loadOlderMessages().then((loadedMessages) => {
            if (loadedMessages.length === 0) {
                pendingScrollRef.current = null;
            }
        });
    }, [hasMoreMessages, isLoadingOlder, loadOlderMessages]);

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

    useEffect(() => {
        initialScrollDoneRef.current = false;
        hasReachedUnreadStartRef.current = false;
        lastSubmittedReadAtRef.current = null;
        pendingScrollRef.current = null;

        if (readTimerRef.current) {
            clearTimeout(readTimerRef.current);
            readTimerRef.current = null;
        }
    }, [conversationId]);

    useLayoutEffect(() => {
        const container = messagesContainerRef.current;

        if (!container || isLoading) return;

        if (pendingScrollRef.current) {
            const previous = pendingScrollRef.current;

            container.scrollTop =
                container.scrollHeight -
                previous.scrollHeight +
                previous.scrollTop;

            pendingScrollRef.current = null;
            return;
        }

        if (!initialScrollDoneRef.current) {
            container.scrollTop = container.scrollHeight;
            initialScrollDoneRef.current = true;
        }
    }, [messages, isLoading]);

    useEffect(() => {
        const container = messagesContainerRef.current;

        if (!container || isLoading || !unreadMessageId) return;

        const observer = new IntersectionObserver(
            (entries) => {
                const unreadEntryVisible = entries.some(
                    (entry) =>
                        entry.isIntersecting &&
                        (entry.target as HTMLElement).dataset.messageId ===
                        unreadMessageId,
                );

                if (unreadEntryVisible) {
                    hasReachedUnreadStartRef.current = true;
                }

                if (!hasReachedUnreadStartRef.current) return;

                const visibleMessages = entries
                    .filter((entry) => entry.isIntersecting)
                    .map((entry) => {
                        const element = entry.target as HTMLElement;

                        return {
                            createdAtUtc: element.dataset.messageCreatedAt,
                        };
                    })
                    .filter(
                        (message): message is { createdAtUtc: string } =>
                            !!message.createdAtUtc,
                    );

                if (visibleMessages.length === 0) return;

                const latestVisible = visibleMessages.reduce((latest, current) =>
                    new Date(current.createdAtUtc).getTime() >
                        new Date(latest.createdAtUtc).getTime()
                        ? current
                        : latest,
                );

                if (
                    lastReadAtUtc &&
                    new Date(latestVisible.createdAtUtc).getTime() <=
                    new Date(lastReadAtUtc).getTime()
                ) {
                    return;
                }

                if (
                    lastSubmittedReadAtRef.current &&
                    new Date(latestVisible.createdAtUtc).getTime() <=
                    new Date(lastSubmittedReadAtRef.current).getTime()
                ) {
                    return;
                }

                if (readTimerRef.current) {
                    clearTimeout(readTimerRef.current);
                }

                readTimerRef.current = setTimeout(async () => {
                    const readAtUtc = latestVisible.createdAtUtc;

                    try {
                        await conversationsApi.markAsRead(
                            conversationId,
                            readAtUtc,
                        );

                        lastSubmittedReadAtRef.current = readAtUtc;
                        onConversationRead?.(conversationId);
                    } catch (requestError) {
                        console.error(
                            "Failed to update conversation read position.",
                            requestError,
                        );
                    } finally {
                        readTimerRef.current = null;
                    }
                }, 300);
            },
            {
                root: container,
                threshold: 0.6,
            },
        );

        const messageElements =
            container.querySelectorAll<HTMLElement>("[data-message-id]");

        messageElements.forEach((element) => observer.observe(element));

        return () => {
            observer.disconnect();

            if (readTimerRef.current) {
                clearTimeout(readTimerRef.current);
                readTimerRef.current = null;
            }
        };
    }, [
        conversationId,
        isLoading,
        messages,
        unreadMessageId,
        lastReadAtUtc,
        onConversationRead,
    ]);

    useEffect(() => {
        return () => {
            if (readTimerRef.current) {
                clearTimeout(readTimerRef.current);
            }
        };
    }, []);

    return (
        <section className="rooms-section channel-chat">
            <div className="rooms-heading">
                <div>
                    <p className="room-eyebrow">
                        {isGroup ? "GROUP CONVERSATION" : "CONVERSATION"}
                    </p>
                    <h2>{conversationName}</h2>
                </div>

                <div className="conversation-chat-actions">
                    <span className="participant-count">
                        {connectionStatus === "connected"
                            ? "Connected"
                            : connectionStatus === "connecting"
                                ? "Connecting..."
                                : connectionStatus === "error"
                                    ? "Connection error"
                                    : "Disconnected"}
                    </span>

                    {isGroup && (
                        <button
                            type="button"
                            onClick={() =>
                                setIsGroupPanelOpen((current) => !current)
                            }
                        >
                            {isGroupPanelOpen ? "Hide details" : "Group details"}
                        </button>
                    )}
                </div>
            </div>

            {isGroup && isGroupPanelOpen && (
                <GroupConversationPanel
                    conversationId={conversationId}
                    currentUserId={currentUserId}
                    onLeave={onLeaveGroup}
                />
            )}

            {error && (
                <p className="form-error" role="alert">
                    {error}
                </p>
            )}

            <div
                ref={messagesContainerRef}
                className="channel-chat-messages"
                onScroll={handleMessagesScroll}
                aria-live="polite"
            >
                <MessageList
                    messages={messages}
                    isLoading={isLoading}
                    isLoadingOlder={isLoadingOlder}
                    unreadMessageId={unreadMessageId}
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
