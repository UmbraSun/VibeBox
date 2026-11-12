
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
import type { MessageResponse } from "@/api/messagesApi";

interface ConversationChatProps {
    conversationId: string;
    conversationName: string;
    conversationType: string;
    lastReadAtUtc: string | null;
    unreadCount: number;
    onLeaveGroup: () => void;
    onConversationRead?: (conversationId: string) => void;
}

export function ConversationChat({
    conversationId,
    conversationName,
    conversationType,
    lastReadAtUtc,
    unreadCount,
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

    const unreadMessages = messages.filter((message) => {
        const isIncoming =
            message.senderId.toLowerCase() !== currentUserId?.toLowerCase();

        if (!isIncoming) return false;

        if (!lastReadAtUtc) return true;

        return (
            new Date(message.createdAtUtc).getTime() >
            new Date(lastReadAtUtc).getTime()
        );
    });

    const unreadMessageId =
        unreadCount > 0 && unreadMessages.length >= unreadCount
            ? unreadMessages[unreadMessages.length - unreadCount]?.id ?? null
            : null;

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

    const [replyToMessage, setReplyToMessage] =
        useState<MessageResponse | null>(null);

    async function handleSend(
        content: string,
        files: File[],
        replyToMessageId?: string,
    ) {
        if (files.length > 0) {
            await sendAttachments(files, content);
        } else {
            await sendMessage(content, replyToMessageId);
        }

        setReplyToMessage(null);
    }

    function handleJumpToMessage(messageId: string) {
        const container = messagesContainerRef.current;

        const target = Array.from(
            container?.querySelectorAll<HTMLElement>(
                "[data-message-id]",
            ) ?? [],
        ).find((element) => element.dataset.messageId === messageId);

        target?.scrollIntoView({
            behavior: "smooth",
            block: "center",
        });
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
        setReplyToMessage(null);

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

        if (!container || !unreadMessageId) {
            return;
        }

        const observer = new IntersectionObserver(
            (entries) => {
                const unreadMessageVisible = entries.some((entry) => {
                    const element = entry.target as HTMLElement;

                    return (
                        element.dataset.messageId === unreadMessageId &&
                        entry.isIntersecting
                    );
                });

                if (unreadMessageVisible) {
                    hasReachedUnreadStartRef.current = true;
                }

                if (!hasReachedUnreadStartRef.current) {
                    return;
                }

                const containerRect = container.getBoundingClientRect();
                const visibleMessageDates: string[] = [];

                container
                    .querySelectorAll<HTMLElement>(
                        ".channel-chat-message[data-message-id]",
                    )
                    .forEach((element: HTMLElement) => {
                        const rect = element.getBoundingClientRect();

                        const isVisible =
                            rect.bottom > containerRect.top &&
                            rect.top < containerRect.bottom;

                        const createdAtUtc = element.dataset.messageCreatedAt;

                        if (isVisible && createdAtUtc) {
                            visibleMessageDates.push(createdAtUtc);
                        }
                    });

                if (visibleMessageDates.length === 0) {
                    return;
                }

                const readAtUtc = visibleMessageDates.reduce(
                    (latest, current) =>
                        new Date(current).getTime() >
                            new Date(latest).getTime()
                            ? current
                            : latest,
                );

                if (
                    lastReadAtUtc &&
                    new Date(readAtUtc).getTime() <=
                    new Date(lastReadAtUtc).getTime()
                ) {
                    return;
                }

                if (
                    lastSubmittedReadAtRef.current &&
                    new Date(readAtUtc).getTime() <=
                    new Date(lastSubmittedReadAtRef.current).getTime()
                ) {
                    return;
                }

                if (readTimerRef.current) {
                    clearTimeout(readTimerRef.current);
                }

                readTimerRef.current = setTimeout(async () => {
                    try {
                        await conversationsApi.markAsRead(
                            conversationId,
                            readAtUtc,
                        );

                        lastSubmittedReadAtRef.current = readAtUtc;
                        onConversationRead?.(conversationId);
                    } catch (error) {
                        console.error(
                            "Failed to mark conversation as read:",
                            error,
                        );
                    }
                }, 300);
            },
            {
                root: container,
                threshold: 0.1,
            },
        );

        const messageElements = container.querySelectorAll<HTMLElement>(
            ".channel-chat-message[data-message-id]",
        );

        messageElements.forEach((element: HTMLElement) => {
            observer.observe(element);
        });

        return () => {
            observer.disconnect();

            if (readTimerRef.current) {
                clearTimeout(readTimerRef.current);
                readTimerRef.current = null;
            }
        };
    }, [
        conversationId,
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
                    onReply={setReplyToMessage}
                    onJumpToMessage={handleJumpToMessage}
                />
            </div>

            <MessageComposer
                isSending={isSending}
                replyToMessage={replyToMessage}
                onCancelReply={() => setReplyToMessage(null)}
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
